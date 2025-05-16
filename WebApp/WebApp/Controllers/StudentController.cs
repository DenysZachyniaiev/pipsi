using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using WebApp.ViewModels;
using WebApp.Data;
using WebApp.Models;
using Microsoft.EntityFrameworkCore;

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
            var classes = context.Classes.ToList();
            var students = context.Students.ToList();
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
            var classToDelete = context.Classes.FirstOrDefault(c => c.Name == className);
            if (classToDelete == null)
                return NotFound();

            var relatedEntries = context.ClassStudents.Where(cs => cs.ClassName == className);
            context.ClassStudents.RemoveRange(relatedEntries);

            // Usuwamy przedmioty tej klasy
            var relatedSubjects = context.Subjects.Where(s => s.ClassName == className);
            context.Subjects.RemoveRange(relatedSubjects);

            context.Classes.Remove(classToDelete);
            context.SaveChanges();

            return RedirectToAction("Classes");
        }

        [HttpPost]
        public IActionResult AssignTeacher(string className, string teacherId)
        {
            var classEntity = context.Classes.FirstOrDefault(c => c.Name == className);
            if (classEntity == null)
                return NotFound();

            classEntity.TeacherId = teacherId;
            context.SaveChanges();

            return RedirectToAction("Classes");
        }

        [HttpPost]
        public IActionResult AssignStudents(string className, List<int> studentIds)
        {
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
            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(subjectName) || string.IsNullOrWhiteSpace(teacherId))
                return BadRequest("Invalid data.");

            var classExists = context.Classes.Any(c => c.Name == className);
            if (!classExists)
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

        [HttpGet]
        public IActionResult GetClassDetails(string className)
        {
            var classEntity = context.Classes.FirstOrDefault(c => c.Name == className);
            if (classEntity == null) return NotFound();

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
                AllStudents = context.Students.ToList(),
                AssignedStudents = context.Students.Where(s => assignedStudentIds.Contains(s.Id)).ToList(),
                Subjects = subjects,
                SubjectTeacherNames = subjects.ToDictionary(
                    s => s.Id,
                    s => teachersDict.ContainsKey(s.TeacherId) ? teachersDict[s.TeacherId] : "Unknown")
            };

            return PartialView("GetClassDetails", viewModel);
        }

        [HttpPost]
        public IActionResult RemoveSubjectFromClass(string className, int subjectId)
        {
            if (string.IsNullOrEmpty(className))
                return BadRequest();

            var subject = context.Subjects
                .FirstOrDefault(s => s.Id == subjectId && s.ClassName == className);

            if (subject == null)
            {
                return NotFound();
            }

            context.Subjects.Remove(subject);
            context.SaveChanges();

            return RedirectToAction("Classes");
        }
    }
}