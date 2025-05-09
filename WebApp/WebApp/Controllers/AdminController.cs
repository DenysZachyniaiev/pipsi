using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? ""
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

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;

        var userRoles = await userManager.GetRolesAsync(user);
        if (!userRoles.Contains(model.Role))
        {
            await userManager.RemoveFromRolesAsync(user, userRoles);
            await userManager.AddToRoleAsync(user, model.Role);
        }

        await userManager.UpdateAsync(user);
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

        await userManager.DeleteAsync(user);
        return RedirectToAction("Index");
    }
}