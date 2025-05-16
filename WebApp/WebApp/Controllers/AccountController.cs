using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.ViewModels;
using WebApp.Services;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> signInManager;
    private readonly UserManager<AppUser> userManager;
    private readonly EmailService emailService;

    public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, EmailService emailService)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.emailService = emailService;
    }

    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            if (!string.IsNullOrEmpty(returnUrl))
                TempData["ReturnUrl"] = returnUrl;

            return View(null);
        }

        var user = await userManager.GetUserAsync(User);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
    [HttpGet]
    public IActionResult Login()
    {
        var returnUrl = TempData["ReturnUrl"] as string;
        if (!string.IsNullOrEmpty(returnUrl))
            TempData["ReturnUrl"] = returnUrl;

        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
        if (result.Succeeded)
        {
            var returnUrl = TempData["ReturnUrl"] as string;
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Account");
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }


    [HttpGet]
    public IActionResult Register()
    {
        var returnUrl = TempData["ReturnUrl"] as string;
        if (!string.IsNullOrEmpty(returnUrl))
            TempData["ReturnUrl"] = returnUrl;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (model.SkipVerification)
        {
            return await FinalizeRegistration(model);
        }

        var code = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("VerificationCode", code);

        var body = $"Your code is: <b>{code}</b>";
        await emailService.SendEmailAsync(model.Email, "WebApp", body);

        return View("ConfirmCode", model);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmCode(RegisterViewModel model)
    {
        var expectedCode = HttpContext.Session.GetString("VerificationCode");

        if (model.VerificationCode != expectedCode)
        {
            ModelState.AddModelError("VerificationCode", "Invalid Code");
            return View("ConfirmCode", model);
        }
        HttpContext.Session.Remove("VerificationCode");

        return await FinalizeRegistration(model);
    }

    private async Task<IActionResult> FinalizeRegistration(RegisterViewModel model)
    {
        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Student");
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Account");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View("Register", model);
    }

}
