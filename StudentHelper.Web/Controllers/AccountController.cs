using Microsoft.AspNetCore.Authorization;
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

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var loginResult = await _authService.LoginAsync(model.Email, model.Password);

        if (!loginResult.Success || !loginResult.Value.HasValue)
        {
            ModelState.AddModelError("", loginResult.Message ?? "Невірний email або пароль");
            return View(model);
        }

        var userId = loginResult.Value.Value;

        var user = await _signInManager.UserManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            await _signInManager.SignInAsync(user, model.RememberMe);
        }

        return RedirectToAction("Index", "Calendar");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var regResult = await _authService.RegisterAsync(
            model.FirstName,
            model.LastName,
            model.Email,
            model.Password);

        if (!regResult.Success)
        {
            ModelState.AddModelError("", regResult.Message);
            return View(model);
        }

        var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
        }

        return RedirectToAction("Index", "Calendar");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _authService.ForgotPasswordAsync(
            model.Email,
            (userId, code) => Url.Action("ResetPassword", "Account",
                new { userId, code },
                protocol: Request.Scheme) ?? string.Empty);

        return View("ForgotPasswordConfirmation");
    }

    [AllowAnonymous]
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

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ResetPasswordAsync(
            model.UserId,
            model.Code,
            model.Password);

        if (result.Success)
        {
            return View("ResetPasswordConfirmation");
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}