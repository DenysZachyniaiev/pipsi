using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;
using WebApp.ViewModels;

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
        if (user == null)
            return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        var isStudent = roles.Contains("Student");

        if (!isStudent)
            return View(user);

        var context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        var studentEntry = context.Students.FirstOrDefault(s => s.Id == user.Id);
        if (studentEntry == null)
            return View(user);

        var assignmentIds = context.AssignmentStudents
            .Where(a => a.StudentId == user.Id)
            .Select(a => a.AssignmentId)
            .ToList();

        var assignments = context.Assignments
            .Where(a => assignmentIds.Contains(a.Id))
            .OrderByDescending(a => a.StartDate)
            .ToList();

        var grades = context.Grades
            .Where(g => g.StudentId == user.Id && g.AssignmentId.HasValue)
            .ToDictionary(g => g.AssignmentId!.Value, g => (int?)g.Value);

        var viewModel = new AccountStudentViewModel
        {
            User = user,
            Student = studentEntry,
            Assignments = assignments,
            Grades = grades
        };

        return View(viewModel);
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

        var result = await signInManager.PasswordSignInAsync(user, model.Password, false, false);
        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Home");
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

        var existingUser = await userManager.FindByEmailAsync(model.Email);

        if (existingUser != null)
        {
            var body = $"Ktoś próbował zarejestrować się na Twój adres email.<br>" +
                       $"Jeśli to Ty - zignoruj tę wiadomość.<br>" +
                       $"Jeśli to nie Ty - możesz zignorować, lub zmienić hasło w ustawieniach.";

            await emailService.SendEmailAsync(model.Email, "Próba rejestracji", body);

            ModelState.AddModelError(string.Empty, "Konto z tym adresem email już istnieje.");
            return View(model);
        }

        if (model.SkipVerification)
        {
            return await FinalizeRegistration(model);
        }

        var code = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("VerificationCode", code);

        var message = $"Twój kod rejestracyjny to: <b>{code}</b>";
        await emailService.SendEmailAsync(model.Email, "Kod weryfikacyjny - WebApp", message);

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
            FirstName = model.FirstName,
            LastName = model.LastName,
            DevSkipVerification = model.SkipVerification
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Student");

            var dbContext = HttpContext.RequestServices.GetRequiredService<WebApp.Data.AppDbContext>();
            dbContext.Students.Add(new Student
            {
                Id = user.Id,
                FirstName = user.FirstName ?? user.UserName ?? "Unknown",
                LastName = user.LastName ?? ""
            });
            await dbContext.SaveChangesAsync();

            var cache = HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            cache.Remove("AllStudents");

            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Account");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View("Register", model);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);
        var code = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("ResetCode", code);

        if (user?.DevSkipVerification == true)
        {
            TempData["AutoFillCode"] = code;
        }
        else
        {
            await emailService.SendEmailAsync(model.Email, "Kod resetowania hasła", $"Twój kod to: <b>{code}</b>");
        }

        return View("ForgotPasswordCode", new ResetPasswordViewModel(model.Email));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmForgotPassword(ResetPasswordViewModel model)
    {
        var expectedCode = HttpContext.Session.GetString("ResetCode");

        if (expectedCode == null)
        {
            ModelState.AddModelError(string.Empty, "Sesja wygasła. Spróbuj ponownie.");
            return RedirectToAction("ForgotPassword");
        }

        if (model.VerificationCode != expectedCode)
        {
            ModelState.AddModelError(string.Empty, "Niepoprawny kod");
            return View("ForgotPasswordCode", model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);

        if (user != null)
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View("ForgotPasswordCode", model);
            }
        }

        HttpContext.Session.Remove("ResetCode");

        TempData["PasswordResetSuccess"] = "Jeśli email był poprawny, hasło zostało zresetowane.";
        return RedirectToAction("Login");
    }
}
