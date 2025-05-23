using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using WebApp.Data;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly AppDbContext context;
        private readonly UserManager<AppUser> userManager;
        private readonly IMemoryCache cache;
        private readonly ILogger<StudentController> logger;

        public StudentController(AppDbContext context, UserManager<AppUser> userManager, IMemoryCache cache, ILogger<StudentController> logger)
        {
            this.context = context;
            this.userManager = userManager;
            this.cache = cache;
            this.logger = logger;
        }

        private bool UserCanEditClass(string className)
        {
            if (User.IsInRole("Admin")) return true;

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return context.Subjects.Any(s => s.ClassName == className && s.TeacherId == userId);
        }

        public IActionResult Index()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Index", "Account", new { returnUrl = Url.Action("Index", "Student") });
            }

            const string cacheKey = "AllStudents";
            if (!cache.TryGetValue(cacheKey, out List<Student> students))
            {
                logger.LogInformation("AllStudents not found in cache, fetching from DB...");
                students = context.Students.ToList();
                cache.Set(cacheKey, students, TimeSpan.FromMinutes(5));
                logger.LogInformation("AllStudents cached.");
            }
            else
            {
                logger.LogInformation("AllStudents loaded from cache.");
            }

            return View(students);
        }

        public async Task<IActionResult> Classes()
        {
            var isAdmin = User.IsInRole("Admin");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            List<Class> classes;
            if (isAdmin)
            {
                classes = context.Classes.ToList();
            }
            else
            {
                var classNames = context.Subjects
                    .Where(s => s.TeacherId == userId)
                    .Select(s => s.ClassName)
                    .Distinct()
                    .ToList();

                classes = context.Classes
                    .Where(c => classNames.Contains(c.Name))
                    .ToList();
            }

            var assignedStudentIds = context.ClassStudents.Select(cs => cs.StudentId).ToHashSet();

            var students = context.Students
                .Where(s => !assignedStudentIds.Contains(s.Id))
                .ToList();


            var teachers = await userManager.GetUsersInRoleAsync("Teacher");

            var classStudentMap = context.ClassStudents
                .GroupBy(cs => cs.ClassName)
                .ToDictionary(g => g.Key, g => g.Select(cs => cs.StudentId).ToList());

            var subjects = context.Subjects.ToList();

            var viewModel = new ClassDetailsViewModel
            {
                Classes = classes,
                AllStudents = students,
                AllTeachers = teachers.ToList(),
                ClassStudentMap = classStudentMap,
                Subjects = subjects
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult AddClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
                return BadRequest("Class name is required.");

            if (context.Classes.Any(c => c.Name == className))
                return Conflict("Class already exists.");

            context.Classes.Add(new Class { Name = className });
            context.SaveChanges();

            return RedirectToAction("Classes");
        }

        [HttpPost]
        public IActionResult DeleteClass(string className)
        {
            if (!UserCanEditClass(className)) return Forbid();

            var classToDelete = context.Classes.FirstOrDefault(c => c.Name == className);
            if (classToDelete == null)
                return NotFound();

            var relatedEntries = context.ClassStudents.Where(cs => cs.ClassName == className);
            context.ClassStudents.RemoveRange(relatedEntries);

            var relatedSubjects = context.Subjects.Where(s => s.ClassName == className);
            context.Subjects.RemoveRange(relatedSubjects);

            context.Classes.Remove(classToDelete);
            context.SaveChanges();

            return RedirectToAction("Classes");
        }

        [HttpPost]
        public IActionResult AssignTeacher(string className, string teacherId)
        {
            if (!UserCanEditClass(className)) return Forbid();

            var classEntity = context.Classes.FirstOrDefault(c => c.Name == className);
            if (classEntity == null)
                return NotFound();

            classEntity.TeacherId = teacherId;
            context.SaveChanges();

            return RedirectToAction("Classes");
        }

        [HttpPost]
        public async Task<IActionResult> AssignStudents(string className, List<string> studentIds)
        {
            if (!UserCanEditClass(className)) return Forbid();

            var existingStudentIds = context.Students.Select(s => s.Id).ToHashSet();

            var missingStudentIds = studentIds.Where(id => !existingStudentIds.Contains(id)).ToList();
            if (missingStudentIds.Any())
            {
                var userAccounts = await userManager.Users
                    .Where(u => missingStudentIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var user in userAccounts)
                {
                    var firstName = user.FirstName ?? user.UserName ?? "Unknown";
                    var lastName = user.LastName ?? "";

                    context.Students.Add(new Student
                    {
                        Id = user.Id,
                        FirstName = firstName,
                        LastName = lastName
                    });
                }

                context.SaveChanges();
            }

            var existing = context.ClassStudents.Where(cs => cs.ClassName == className);
            context.ClassStudents.RemoveRange(existing);

            foreach (var studentId in studentIds)
            {
                context.ClassStudents.Add(new ClassStudents
                {
                    ClassName = className,
                    StudentId = studentId
                });
            }

            context.SaveChanges();
            return RedirectToAction("Classes");
        }

        [HttpPost]
        public IActionResult AddSubjectToClass(string className, string subjectName, string teacherId)
        {
            if (!UserCanEditClass(className)) return Forbid();

            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(subjectName) || string.IsNullOrWhiteSpace(teacherId))
                return BadRequest("Invalid data.");

            if (!context.Classes.Any(c => c.Name == className))
                return BadRequest("Class does not exist.");

            var subject = new Subject
            {
                Name = subjectName,
                ClassName = className,
                TeacherId = teacherId
            };

            context.Subjects.Add(subject);
            context.SaveChanges();

            return RedirectToAction("Classes");
        }

        [HttpPost]
        public IActionResult RemoveSubjectFromClass(string className, int subjectId)
        {
            if (!UserCanEditClass(className)) return Forbid();

            if (string.IsNullOrEmpty(className))
                return BadRequest();

            var subject = context.Subjects
                .FirstOrDefault(s => s.Id == subjectId && s.ClassName == className);

            if (subject == null)
                return NotFound();

            context.Subjects.Remove(subject);
            context.SaveChanges();

            return RedirectToAction("Classes");
        }

        [HttpGet]
        public IActionResult GetClassDetails(string className)
        {
            var classEntity = context.Classes.FirstOrDefault(c => c.Name == className);
            if (classEntity == null) return NotFound();

            var isAdmin = User.IsInRole("Admin");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!isAdmin)
            {
                var teachesHere = context.Subjects.Any(s =>
                    s.ClassName == className &&
                    s.TeacherId == userId);

                if (!teachesHere)
                {
                    return Forbid();
                }
            }

            var assignedStudentIds = context.ClassStudents
                .Where(cs => cs.ClassName == className)
                .Select(cs => cs.StudentId)
                .ToList();

            var subjects = context.Subjects.Where(s => s.ClassName == className).ToList();
            var teachers = userManager.GetUsersInRoleAsync("Teacher").Result.ToList();
            var teachersDict = teachers.ToDictionary(t => t.Id, t => t.UserName);

            var viewModel = new ClassDetailsViewModel
            {
                ClassName = classEntity.Name,
                TeacherId = classEntity.TeacherId,
                AllTeachers = teachers,
                AllStudents = context.Students
                    .Where(s =>
                        !context.ClassStudents.Any(cs => cs.StudentId == s.Id && cs.ClassName != className)
                    )
                    .ToList(),
                AssignedStudents = context.Students.Where(s => assignedStudentIds.Contains(s.Id)).ToList(),
                Subjects = subjects,
                SubjectTeacherNames = subjects.ToDictionary(
                    s => s.Id,
                    s => teachersDict.ContainsKey(s.TeacherId) ? teachersDict[s.TeacherId] : "Unknown")
            };

            return PartialView("GetClassDetails", viewModel);
        }
    }
}