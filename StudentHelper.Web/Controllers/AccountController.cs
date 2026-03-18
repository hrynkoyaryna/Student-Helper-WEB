using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces;
using StudentHelper.Web.Models;

namespace StudentHelper.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

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

        var success = await _authService.RegisterAsync(
            model.FirstName, model.LastName, model.Email, model.Password, 1);

        if (!success)
        {
            ModelState.AddModelError("Email", "Користувач з таким email вже існує");
            return View(model);
        }

        return RedirectToAction("Index", "Home");
    }
}
