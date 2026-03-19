using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models;
using System.Text;
using System.Web;

namespace StudentHelper.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IEmailSender emailSender,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailSender = emailSender;
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

        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Невірний email або пароль");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Користувач {user.Email} успішно увійшов");
                return RedirectToAction("Index", "Calendar");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Акаунт заблокований. Спробуйте пізніше");
                return View(model);
            }

            ModelState.AddModelError("", "Невірний email або пароль");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Помилка під час входу");
            ModelState.AddModelError("", "При вході сталась помилка. Спробуйте ще раз.");
        }

        return View(model);
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

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Користувач з таким email вже зареєстрований");
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Користувач {user.Email} успішно зареєстрований");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Calendar");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Помилка під час реєстрації користувача");
            ModelState.AddModelError("", "При реєстрації сталась помилка. Спробуйте ще раз.");
        }

        return View(model);
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

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return View("ForgotPasswordConfirmation");
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Action("ResetPassword", "Account", 
            new { userId = user.Id, code = code }, 
            protocol: Request.Scheme);

        await _emailSender.SendEmailAsync(user.Email ?? "", "Скидання пароля",
            $"Клацніть на посилання для скидання пароля: <a href=\"{callbackUrl}\">тут</a>");

        _logger.LogInformation($"Для користувача {user.Email} надіслано посилання для скидання пароля");
        
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

        var user = await _userManager.FindByIdAsync(model.UserId.ToString());
        if (user == null)
        {
            ModelState.AddModelError("", "Користувача не знайдено");
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation($"Користувач {user.Email} скинув пароль");
            return View("ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(model);
    }
}