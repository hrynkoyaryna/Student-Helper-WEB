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
        var loginResult = await _authService.LoginAsync(model.Email, model.Password);

        if (!loginResult.Success || !loginResult.Value.HasValue)
        {
            ModelState.AddModelError("", loginResult.Message ?? "Невірний email або пароль");
            return View(model);
        }

        var userId = loginResult.Value.Value;

        // Логіка сесії залишається в контролері (це не use-case)
        var user = await _signInManager.UserManager.FindByIdAsync(userId.ToString());
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
        var regResult = await _authService.RegisterAsync(
            model.FirstName,
            model.LastName,
            model.Email,
            model.Password);

        if (!regResult.Success)
        {
            // If the service returned structured errors in Value, use them; otherwise use Message
            if (regResult.Value != null && regResult.Value.Any())
            {
                foreach (var error in regResult.Value)
                {
                    ModelState.AddModelError("", error);
                }
            }
            else
            {
                ModelState.AddModelError("", regResult.Message);
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
        var result = await _authService.ForgotPasswordAsync(
            model.Email,
            (userId, code) => Url.Action("ResetPassword", "Account",
                new { userId, code },
                protocol: Request.Scheme) ?? string.Empty);

        _logger.LogInformation($"Запит на скидання пароля для email: {model.Email}");
        // We show confirmation regardless to avoid user enumeration. Optionally display message from result.
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
        var result = await _authService.ResetPasswordAsync(
            model.UserId,
            model.Code,
            model.Password);

        if (result.Success)
        {
            _logger.LogInformation($"Користувач з ID {model.UserId} успішно скинув пароль");
            return View("ResetPasswordConfirmation");
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }
}
