using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using WebApp.Data;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class GradesController : Controller
    {
        private readonly AppDbContext context;

        public GradesController(AppDbContext context)
        {
            this.context = context;
        }

        public IActionResult ManageGrades(string className, string? weekStart)
        {
            if (string.IsNullOrEmpty(className))
            {
                return BadRequest("Class name is required.");
            }

            DateTime? parsedWeekStart = null;
            if (!string.IsNullOrEmpty(weekStart) && TryParseIsoWeek(weekStart, out var ws))
            {
                parsedWeekStart = ws;
            }

            var studentIds = context.ClassStudents
                .Where(cs => cs.ClassName == className)
                .Select(cs => cs.StudentId)
                .ToList();

            var students = context.Students
                .Where(s => studentIds.Contains(s.Id))
                .ToList();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            var allowedSubjectNames = isAdmin
                ? null
                : context.Subjects
                    .Where(s => s.ClassName == className && s.TeacherId == userId)
                    .Select(s => s.Name)
                    .ToHashSet();

            var assignmentsQuery = context.Assignments
                .Where(a => a.ClassName == className);

            if (!isAdmin)
            {
                assignmentsQuery = assignmentsQuery
                    .Where(a => allowedSubjectNames.Contains(a.SubjectName));
            }

            if (parsedWeekStart.HasValue)
            {
                var end = parsedWeekStart.Value.AddDays(7);
                assignmentsQuery = assignmentsQuery
                    .Where(a => a.StartDate >= parsedWeekStart && a.StartDate < end);
            }

            var assignments = assignmentsQuery.ToList();
            var assignmentIds = assignments.Select(a => a.Id).ToList();

            var assignmentStudentMap = context.AssignmentStudents
                .Where(x => assignmentIds.Contains(x.AssignmentId))
                .GroupBy(x => x.AssignmentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.StudentId).ToHashSet()
                );

            var grades = context.Grades
                .Where(g => g.AssignmentId.HasValue && assignmentIds.Contains(g.AssignmentId.Value))
                .GroupBy(g => g.AssignmentId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(gr => gr.StudentId, gr => (int?)gr.Value)
                );

            var viewModel = new ManageGradesViewModel
            {
                ClassName = className,
                Students = students,
                Assignments = assignments,
                AssignmentStudentMap = assignmentStudentMap,
                Grades = grades,
                WeekStart = parsedWeekStart
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult AddGrade(string className)
        {
            var studentIds = context.ClassStudents
                .Where(cs => cs.ClassName == className)
                .Select(cs => cs.StudentId)
                .ToList();

            var students = context.Students
                .Where(s => studentIds.Contains(s.Id))
                .ToList();

            var subjects = context.Subjects
                .Where(s => s.ClassName == className)
                .ToList();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userIsAdmin = User.IsInRole("Admin");

            var teacherSubject = !userIsAdmin
                ? subjects.FirstOrDefault(s => s.TeacherId == userId)
                : null;

            ViewBag.ClassName = className;
            ViewBag.Students = students;
            ViewBag.Subjects = subjects;
            ViewBag.TeacherSubject = teacherSubject;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult AddGrade(Grade grade, string className)
        {
            if (ModelState.IsValid)
            {
                grade.Date = DateTime.Now;
                context.Grades.Add(grade);
                context.SaveChanges();

                return RedirectToAction("ManageGrades", new
                {
                    className,
                    weekStart = ISOFormat(grade.Date)
                });
            }

            var studentIds = context.ClassStudents
                .Where(cs => cs.ClassName == className)
                .Select(cs => cs.StudentId)
                .ToList();

            ViewBag.ClassName = className;
            ViewBag.Students = context.Students
                .Where(s => studentIds.Contains(s.Id))
                .ToList();

            return View(grade);
        }

        private static bool TryParseIsoWeek(string isoWeek, out DateTime weekStart)
        {
            weekStart = default;
            var match = Regex.Match(isoWeek, @"^(\d{4})-W(\d{2})$");
            if (!match.Success) return false;

            int year = int.Parse(match.Groups[1].Value);
            int week = int.Parse(match.Groups[2].Value);

            var jan4 = new DateTime(year, 1, 4);
            int dayOfWeek = ((int)jan4.DayOfWeek + 6) % 7;
            var firstMonday = jan4.AddDays(-dayOfWeek);
            weekStart = firstMonday.AddDays((week - 1) * 7);
            return true;
        }

        private static string ISOFormat(DateTime? date)
        {
            return date?.ToString("yyyy-'W'ww") ?? "";
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult SaveGrades(string className, Dictionary<int, Dictionary<int, string>> Grades)
        {
            int changes = 0;
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            HashSet<string>? allowedSubjects = null;
            if (!isAdmin)
            {
                allowedSubjects = context.Subjects
                    .Where(s => s.ClassName == className && s.TeacherId == userId)
                    .Select(s => s.Name)
                    .ToHashSet();
            }

            foreach (var assignmentEntry in Grades)
            {
                var assignmentId = assignmentEntry.Key;

                if (!isAdmin)
                {
                    var assignment = context.Assignments.FirstOrDefault(a => a.Id == assignmentId);
                    if (assignment == null || !allowedSubjects!.Contains(assignment.SubjectName))
                    {
                        continue;
                    }
                }

                foreach (var studentEntry in assignmentEntry.Value)
                {
                    var studentId = studentEntry.Key;
                    var valueStr = studentEntry.Value?.Trim();

                    var existing = context.Grades.FirstOrDefault(g =>
                        g.AssignmentId == assignmentId && g.StudentId == studentId);

                    if (string.IsNullOrWhiteSpace(valueStr))
                    {
                        if (existing != null)
                        {
                            context.Grades.Remove(existing);
                            changes++;
                        }
                    }
                    else if (int.TryParse(valueStr, out int value))
                    {
                        if (existing != null)
                        {
                            if (existing.Value != value)
                            {
                                existing.Value = value;
                                existing.Date = DateTime.Now;
                                changes++;
                            }
                        }
                        else
                        {
                            context.Grades.Add(new Grade
                            {
                                AssignmentId = assignmentId,
                                StudentId = studentId,
                                Value = value,
                                Date = DateTime.Now
                            });
                            changes++;
                        }
                    }
                }
            }

            if (changes > 0)
            {
                context.SaveChanges();
                TempData["SuccessMessage"] = "Oceny zostały zapisane.";
            }

            return RedirectToAction("ManageGrades", new { className });
        }
    }
}