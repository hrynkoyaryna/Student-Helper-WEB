using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Settings;

namespace StudentHelper.Web.Controllers;

public class SettingsController : BaseController
{
    private readonly IUserService _userService;
    private readonly IAccountService _accountService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IUserService userService,
        IAccountService accountService,
        ILogger<SettingsController> logger)
    {
        _userService = userService;
        _accountService = accountService;
        _logger = logger;
    }

    private async Task<User> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Користувач не знайдений.");
        }

        return user;
    }

    // ========== INDEX ==========
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();

        var model = new SettingsIndexViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        };

        return View(model);
    }

    // ========== CHANGE PASSWORD ==========
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.NewPassword != model.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Паролі не збігаються");
            return View(model);
        }

        var user = await GetCurrentUserAsync();

        var result = await _accountService.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }
}
