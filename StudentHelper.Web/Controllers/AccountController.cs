using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models;
using StudentHelper.Application.Interfaces;

namespace StudentHelper.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<User> signInManager,
        IAuthService authService,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _authService = authService;
        _logger = logger;
    }

    // ========== LOGIN ==========
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // USE-CASE: Login - делегуємо всю логіку до сервісу
        var (success, userId, message) = await _authService.LoginAsync(model.Email, model.Password);

        if (!success || !userId.HasValue)
        {
            ModelState.AddModelError("", message);
            return View(model);
        }

        // Логіка сесії залишається в контролері (це не use-case)
        var user = await _signInManager.UserManager.FindByIdAsync(userId.Value.ToString());
        if (user != null)
        {
            await _signInManager.SignInAsync(user, model.RememberMe);
        }

        _logger.LogInformation($"Користувач з ID {userId} успішно увійшов");
        return RedirectToAction("Index", "Calendar");
    }

    // ========== LOGOUT ==========
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Користувач розійшовся");
        return RedirectToAction("Index", "Home");
    }

    // ========== REGISTER ==========
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // USE-CASE: Register - делегуємо всю логіку до сервісу
        var (success, message, errors) = await _authService.RegisterAsync(
            model.FirstName,
            model.LastName,
            model.Email,
            model.Password);

        if (!success)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError("", error);
            }
            return View(model);
        }

        // Логіка сесії залишається в контролері (це не use-case)
        var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
        }

        _logger.LogInformation($"Користувач {model.Email} успішно зареєстрований");
        return RedirectToAction("Index", "Calendar");
    }

    // ========== FORGOT PASSWORD ==========
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // USE-CASE: ForgotPassword - делегуємо всю логіку до сервісу
        var (success, message, _) = await _authService.ForgotPasswordAsync(
            model.Email,
            (userId, code) => Url.Action("ResetPassword", "Account",
                new { userId, code },
                protocol: Request.Scheme) ?? string.Empty);

        _logger.LogInformation($"Запит на скидання пароля для email: {model.Email}");
        return View("ForgotPasswordConfirmation");
    }

    // ========== RESET PASSWORD ==========
    [HttpGet]
    public IActionResult ResetPassword(int? userId, string code)
    {
        if (userId == null || string.IsNullOrEmpty(code))
        {
            return RedirectToAction("ForgotPassword");
        }

        var model = new ResetPasswordViewModel { UserId = userId.Value, Code = code };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // USE-CASE: ResetPassword - делегуємо всю логіку до сервісу
        var (success, message) = await _authService.ResetPasswordAsync(
            model.UserId,
            model.Code,
            model.Password);

        if (success)
        {
            _logger.LogInformation($"Користувач з ID {model.UserId} успішно скинув пароль");
            return View("ResetPasswordConfirmation");
        }

        ModelState.AddModelError("", message);
        return View(model);
    }
}
