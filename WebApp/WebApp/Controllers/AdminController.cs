using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Models;
using WebApp.ViewModels;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<AppUser> userManager;
    private readonly RoleManager<IdentityRole> roleManager;

    public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        this.userManager = userManager;
        this.roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = userManager.Users.ToList();
        var userWithRoles = new List<(AppUser User, string Role)>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "None";
            userWithRoles.Add((user, role));
        }

        return View(userWithRoles);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            Role = roles.FirstOrDefault() ?? "",
            StudentFirstName = user.FirstName,
            StudentLastName = user.LastName
        };

        ViewBag.AllRoles = roleManager.Roles.Select(r => r.Name).ToList();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        var user = await userManager.FindByIdAsync(model.Id);
        if (user == null)
            return NotFound();

        user.Email = model.Email;
        user.UserName = model.Email;
        user.FirstName = model.StudentFirstName;
        user.LastName = model.StudentLastName;

        var userRoles = await userManager.GetRolesAsync(user);
        var currentRole = userRoles.FirstOrDefault();

        if (currentRole != model.Role)
        {
            if (!string.IsNullOrEmpty(currentRole))
                await userManager.RemoveFromRoleAsync(user, currentRole);

            await userManager.AddToRoleAsync(user, model.Role);
        }

        await userManager.UpdateAsync(user);

        var dbContext = HttpContext.RequestServices.GetRequiredService<WebApp.Data.AppDbContext>();
        var memoryCache = HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

        var studentEntry = dbContext.Students.FirstOrDefault(s => s.Id == user.Id);

        if (model.Role == "Student" && studentEntry == null)
        {
            dbContext.Students.Add(new Student
            {
                Id = user.Id,
                FirstName = user.FirstName ?? user.UserName ?? "Unknown",
                LastName = user.LastName ?? ""
            });
        }
        else if (model.Role != "Student" && studentEntry != null)
        {
            var grades = dbContext.Grades.Where(g => g.StudentId == user.Id);
            var classLinks = dbContext.ClassStudents.Where(cs => cs.StudentId == user.Id);
            var assignmentLinks = dbContext.AssignmentStudents.Where(a => a.StudentId == user.Id);

            dbContext.Grades.RemoveRange(grades);
            dbContext.ClassStudents.RemoveRange(classLinks);
            dbContext.AssignmentStudents.RemoveRange(assignmentLinks);
            dbContext.Students.Remove(studentEntry);
        }

        await dbContext.SaveChangesAsync();

        // Inwalidacja cache uczniów
        memoryCache.Remove("AllStudents");

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var userRoles = await userManager.GetRolesAsync(user);
        var isStudent = userRoles.Contains("Student");

        var dbContext = HttpContext.RequestServices.GetRequiredService<WebApp.Data.AppDbContext>();
        var memoryCache = HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

        if (isStudent)
        {
            var studentEntry = dbContext.Students.FirstOrDefault(s => s.Id == user.Id);
            if (studentEntry != null)
            {
                var grades = dbContext.Grades.Where(g => g.StudentId == user.Id);
                var classLinks = dbContext.ClassStudents.Where(cs => cs.StudentId == user.Id);
                var assignmentLinks = dbContext.AssignmentStudents.Where(a => a.StudentId == user.Id);

                dbContext.Grades.RemoveRange(grades);
                dbContext.ClassStudents.RemoveRange(classLinks);
                dbContext.AssignmentStudents.RemoveRange(assignmentLinks);
                dbContext.Students.Remove(studentEntry);
            }

            await dbContext.SaveChangesAsync();
        }

        // Inwalidacja cache uczniów
        memoryCache.Remove("AllStudents");

        await userManager.DeleteAsync(user);
        return RedirectToAction("Index");
    }
}