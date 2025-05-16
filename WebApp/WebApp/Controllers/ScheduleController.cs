using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize]
    public class ScheduleController: Controller
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

        public IActionResult ViewSchedule(string className)
        {
            if (string.IsNullOrEmpty(className))
                return RedirectToAction("Index");

            var entries = context.ScheduleEntries
                .Where(e => context.Subjects
                    .Where(s => s.ClassName == className)
                    .Select(s => s.Id)
                    .Contains(e.SubjectId))
                .OrderBy(e => e.Day)
                .ThenBy(e => e.Hour)
                .ToList();

            ViewBag.ClassName = className;
            return View(entries);
        }

        private void PopulateDays()
        {
            ViewBag.Days = Enum.GetValues(typeof(DayOfWeek))
                .Cast<DayOfWeek>()
                .Select(d => new SelectListItem
                {
                    Text = d.ToString(),
                    Value = ((int)d).ToString()
                }).ToList();
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

        [Authorize(Roles = "Admin")]
        public IActionResult Create(string className)
        {
            if (string.IsNullOrEmpty(className))
                return RedirectToAction(nameof(ViewSchedule));

            PopulateDays();
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
                PopulateDays();
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

            PopulateDays();
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
                PopulateDays();
                PopulateSubjects(className);
                ViewBag.ClassName = className;
                return View(model);
            }

            context.ScheduleEntries.Update(model);
            context.SaveChanges();

            return RedirectToAction(nameof(ViewSchedule), new { className });
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id, string className)
        {
            var entry = context.ScheduleEntries.Find(id);
            if (entry == null)
                return NotFound();

            context.ScheduleEntries.Remove(entry);
            context.SaveChanges();

            return RedirectToAction(nameof(ViewSchedule), new { className });
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var entry = context.ScheduleEntries.Find(id);
            if (entry == null)
                return NotFound();

            var subject = context.Subjects.FirstOrDefault(s => s.Id == entry.SubjectId);
            ViewBag.SubjectName = subject?.Name ?? "Unknown";

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

            context.ScheduleEntries.Remove(entry);
            context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public JsonResult GetSubjectsByClass(string className)
        {
            var subjects = context.Subjects.Where(s => s.ClassName == className).ToList();
            return Json(subjects);
        }
    }
}