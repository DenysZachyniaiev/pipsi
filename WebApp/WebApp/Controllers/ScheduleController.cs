using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebApp.Data;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly AppDbContext context;

        public ScheduleController(AppDbContext context)
        {
            this.context = context;
        }

        public IActionResult Index()
        {
            var classes = context.Classes.OrderBy(c => c.Name).ToList();
            return View(classes);
        }

        public IActionResult ViewSchedule(string className, string? weekStart)
        {
            if (string.IsNullOrEmpty(className))
                return RedirectToAction("Index");

            DateTime weekStartDate;
            if (!string.IsNullOrEmpty(weekStart) && TryParseIsoWeek(weekStart, out var parsedDate))
            {
                weekStartDate = parsedDate;
            }
            else
            {
                var today = DateTime.Today;
                weekStartDate = ISOWeek.ToDateTime(ISOWeek.GetYear(today), ISOWeek.GetWeekOfYear(today), DayOfWeek.Monday);
            }

            var weekEndDate = weekStartDate.AddDays(6);

            var subjectsForClass = context.Subjects
                .Where(s => s.ClassName == className)
                .ToDictionary(s => s.Id, s => s);

            var entries = context.ScheduleEntries
                .Where(e =>
                    subjectsForClass.Keys.Contains(e.SubjectId) &&
                    e.Date >= weekStartDate &&
                    e.Date <= weekEndDate)
                .ToList();

            var viewModel = new WeeklyScheduleViewModel
            {
                ClassName = className,
                WeekStart = weekStartDate,
                ScheduleEntries = entries.Select(e => new ScheduleEntryDisplay
                {
                    Id = e.Id,
                    Date = e.Date,
                    Day = e.Day,
                    Hour = e.Hour,
                    SubjectName = subjectsForClass.TryGetValue(e.SubjectId, out var subj) ? subj.Name : "Unknown",
                    ClassroomNumber = e.ClassroomNumber
                }).ToList()
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create(string className)
        {
            if (string.IsNullOrEmpty(className))
                return RedirectToAction(nameof(Index));

            PopulateSubjects(className);
            ViewBag.ClassName = className;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(ScheduleEntry model, string className)
        {
            if (!ModelState.IsValid)
            {
                PopulateSubjects(className);
                ViewBag.ClassName = className;
                return View(model);
            }

            model.Day = model.Date.DayOfWeek;

            var existing = context.ScheduleEntries
                .Where(e => e.Date == model.Date && e.Hour == model.Hour)
                .Join(context.Subjects,
                      entry => entry.SubjectId,
                      subject => subject.Id,
                      (entry, subject) => new { entry, subject })
                .FirstOrDefault(joined => joined.subject.ClassName == className);

            if (existing != null)
            {
                ModelState.AddModelError("", "Another lesson already exists at this time for this class.");
                PopulateSubjects(className);
                ViewBag.ClassName = className;
                return View(model);
            }

            context.ScheduleEntries.Add(model);
            context.SaveChanges();

            return RedirectToAction(nameof(ViewSchedule), new { className });
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id, string className)
        {
            var entry = context.ScheduleEntries.Find(id);
            if (entry == null)
                return NotFound();

            PopulateSubjects(className);
            ViewBag.ClassName = className;

            return View(entry);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(ScheduleEntry model, string className)
        {
            if (!ModelState.IsValid)
            {
                PopulateSubjects(className);
                ViewBag.ClassName = className;
                return View(model);
            }

            model.Day = model.Date.DayOfWeek;

            var existing = context.ScheduleEntries
                .Where(e => e.Id != model.Id && e.Date == model.Date && e.Hour == model.Hour)
                .Join(context.Subjects,
                      entry => entry.SubjectId,
                      subject => subject.Id,
                      (entry, subject) => new { entry, subject })
                .FirstOrDefault(joined => joined.subject.ClassName == className);

            if (existing != null)
            {
                ModelState.AddModelError("", "Another lesson already exists at this time for this class.");
                PopulateSubjects(className);
                ViewBag.ClassName = className;
                return View(model);
            }

            context.ScheduleEntries.Update(model);
            context.SaveChanges();

            return RedirectToAction(nameof(ViewSchedule), new { className });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var entry = context.ScheduleEntries.FirstOrDefault(e => e.Id == id);
            if (entry == null)
                return NotFound();

            var subject = context.Subjects.FirstOrDefault(s => s.Id == entry.SubjectId);
            ViewBag.SubjectName = subject?.Name ?? "Unknown";
            ViewBag.ClassName = subject?.ClassName ?? "";

            return View(entry);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var entry = context.ScheduleEntries.Find(id);
            if (entry == null)
                return NotFound();

            var className = context.Subjects
                .Where(s => s.Id == entry.SubjectId)
                .Select(s => s.ClassName)
                .FirstOrDefault() ?? "";

            context.ScheduleEntries.Remove(entry);
            context.SaveChanges();

            return RedirectToAction(nameof(ViewSchedule), new { className });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public JsonResult GetSubjectsByClass(string className)
        {
            var subjects = context.Subjects
                .Where(s => s.ClassName == className)
                .Select(s => new { s.Id, s.Name })
                .ToList();

            return Json(subjects);
        }

        private void PopulateSubjects(string className)
        {
            ViewBag.Subjects = context.Subjects
                .Where(s => s.ClassName == className)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                }).ToList();
        }

        private static bool TryParseIsoWeek(string isoWeek, out DateTime weekStart)
        {
            weekStart = default;
            var match = System.Text.RegularExpressions.Regex.Match(isoWeek, @"^(\d{4})-W(\d{2})$");
            if (!match.Success) return false;

            int year = int.Parse(match.Groups[1].Value);
            int week = int.Parse(match.Groups[2].Value);

            weekStart = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
            return true;
        }
    }
}