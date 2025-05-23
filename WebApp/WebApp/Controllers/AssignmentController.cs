using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Data;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class AssignmentController : Controller
    {
        private readonly AppDbContext context;

        public AssignmentController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public IActionResult Create(string className)
        {
            var studentIds = context.ClassStudents
                .Where(cs => cs.ClassName == className)
                .Select(cs => cs.StudentId)
                .ToList();

            var students = context.Students
                .Where(s => studentIds.Contains(s.Id))
                .ToList();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            var subjects = isAdmin
                ? context.Subjects.Where(s => s.ClassName == className).ToList()
                : context.Subjects.Where(s => s.ClassName == className && s.TeacherId == userId).ToList();

            var viewModel = new CreateAssignmentViewModel
            {
                ClassName = className,
                AllStudents = students,
                StartDate = DateTime.Today
            };

            ViewBag.Subjects = subjects;
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Create(CreateAssignmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllStudents = context.ClassStudents
                    .Where(cs => cs.ClassName == model.ClassName)
                    .Join(context.Students, cs => cs.StudentId, s => s.Id, (cs, s) => s)
                    .ToList();

                return View(model);
            }

            if (!User.IsInRole("Admin"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var subject = context.Subjects.FirstOrDefault(s =>
                    s.Name == model.SubjectName &&
                    s.ClassName == model.ClassName);

                if (subject == null || subject.TeacherId != userId)
                {
                    return Forbid();
                }
            }

            var validStudentIds = context.ClassStudents
                .Where(cs => cs.ClassName == model.ClassName)
                .Select(cs => cs.StudentId)
                .ToHashSet();

            var invalidStudentIds = model.SelectedStudentIds
                .Where(id => !validStudentIds.Contains(id))
                .ToList();

            if (invalidStudentIds.Any())
            {
                ModelState.AddModelError("", "Wszyscy przypisani uczniowie muszą należeć do jednej klasy.");
                model.AllStudents = context.ClassStudents
                    .Where(cs => cs.ClassName == model.ClassName)
                    .Join(context.Students, cs => cs.StudentId, s => s.Id, (cs, s) => s)
                    .ToList();

                return View(model);
            }

            var className = model.ClassName;

            var assignment = new Assignment
            {
                Name = model.Name,
                Type = model.Type,
                SubjectName = model.SubjectName,
                StartDate = model.StartDate,
                DueDate = model.DueDate,
                ClassName = className
            };

            context.Assignments.Add(assignment);
            context.SaveChanges();

            var assignmentStudents = model.SelectedStudentIds.Select(studentId => new AssignmentStudent
            {
                AssignmentId = assignment.Id,
                StudentId = studentId
            });

            context.AssignmentStudents.AddRange(assignmentStudents);
            context.SaveChanges();

            return RedirectToAction("ManageGrades", "Grades", new { className = className });
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var assignment = context.Assignments.FirstOrDefault(a => a.Id == id);
            if (assignment == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                var subject = context.Subjects.FirstOrDefault(s =>
                    s.Name == assignment.SubjectName &&
                    s.ClassName == assignment.ClassName);

                if (subject == null || subject.TeacherId != userId)
                {
                    return Forbid();
                }
            }

            var studentIds = context.ClassStudents
                .Where(cs => cs.ClassName == assignment.ClassName)
                .Select(cs => cs.StudentId)
                .ToList();

            var students = context.Students
                .Where(s => studentIds.Contains(s.Id))
                .ToList();

            var selectedStudentIds = context.AssignmentStudents
                .Where(x => x.AssignmentId == assignment.Id)
                .Select(x => x.StudentId)
                .ToList();

            var subjects = isAdmin
                ? context.Subjects.Where(s => s.ClassName == assignment.ClassName).ToList()
                : context.Subjects.Where(s => s.ClassName == assignment.ClassName && s.TeacherId == userId).ToList();

            var viewModel = new EditAssignmentViewModel
            {
                Id = assignment.Id,
                Name = assignment.Name,
                Type = assignment.Type,
                SubjectName = assignment.SubjectName,
                StartDate = assignment.StartDate,
                DueDate = assignment.DueDate,
                ClassName = assignment.ClassName,
                AllStudents = students,
                SelectedStudentIds = selectedStudentIds,
                AvailableSubjects = subjects
            };

            return View(viewModel);
        }


        [HttpPost]
        public IActionResult Edit(EditAssignmentViewModel model)
        {
            var assignment = context.Assignments.FirstOrDefault(a => a.Id == model.Id);
            if (assignment == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                var subject = context.Subjects.FirstOrDefault(s =>
                    s.Name == model.SubjectName &&
                    s.ClassName == model.ClassName);

                if (subject == null || subject.TeacherId != userId)
                {
                    return Forbid();
                }
            }

            assignment.Name = model.Name;
            assignment.Type = model.Type;
            assignment.StartDate = model.StartDate;
            assignment.DueDate = model.DueDate;
            assignment.SubjectName = model.SubjectName;

            var existingLinks = context.AssignmentStudents.Where(x => x.AssignmentId == assignment.Id);
            context.AssignmentStudents.RemoveRange(existingLinks);

            var newLinks = model.SelectedStudentIds.Select(studentId => new AssignmentStudent
            {
                AssignmentId = assignment.Id,
                StudentId = studentId
            });

            context.AssignmentStudents.AddRange(newLinks);

            context.SaveChanges();

            return RedirectToAction("ManageGrades", "Grades", new { className = model.ClassName });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var assignment = context.Assignments.FirstOrDefault(a => a.Id == id);
            if (assignment == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                var subject = context.Subjects.FirstOrDefault(s =>
                    s.Name == assignment.SubjectName &&
                    s.ClassName == assignment.ClassName);

                if (subject == null || subject.TeacherId != userId)
                {
                    return Forbid();
                }
            }

            var relatedGrades = context.Grades.Where(g => g.AssignmentId == assignment.Id);
            var relatedLinks = context.AssignmentStudents.Where(x => x.AssignmentId == assignment.Id);

            context.Grades.RemoveRange(relatedGrades);
            context.AssignmentStudents.RemoveRange(relatedLinks);
            context.Assignments.Remove(assignment);
            context.SaveChanges();

            return RedirectToAction("ManageGrades", "Grades", new { className = assignment.ClassName });
        }
    }
}