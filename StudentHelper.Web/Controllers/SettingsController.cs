using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Settings;
using System;
using System.Threading.Tasks;

namespace StudentHelper.Web.Controllers;

[Authorize]
public class SettingsController : BaseController
{
    private readonly IUserService _userService;
    private readonly IAccountService _accountService;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IUserService userService,
        IAccountService accountService,
        SignInManager<User> signInManager,
        ILogger<SettingsController> logger)
    {
        _userService = userService;
        _accountService = accountService;
        _signInManager = signInManager;
        _logger = logger;
    }

    private async Task<User> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Користувач не знайдений");
        }

        return user;
    }

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

        ModelState.AddModelError(string.Empty, result.Message);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var user = await GetCurrentUserAsync();

        var model = new EditProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        };
        
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        var result = await _userService.UpdateProfileAsync(userId, model.FirstName, model.LastName, model.Email);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Профіль успішно оновлено";
            return RedirectToAction("Index");
        }

        ModelState.AddModelError(string.Empty, result.Message);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _userService.DeleteUserAsync(userId);

        if (result.Success)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction("Index");
    }
}