using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces;

namespace StudentHelper.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly IUserService _userService;
    private readonly IScheduleRepository _scheduleRepository;

    public AdminController(IUserService userService, IScheduleRepository scheduleRepository)
    {
        _userService = userService;
        _scheduleRepository = scheduleRepository;
    }

    public async Task<IActionResult> Index()
    {
        var groups = await _scheduleRepository.GetAllGroupsAsync();
        return View(groups); // Передаємо список груп у представлення
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(string name)
    {
        var result = await _userService.CreateGroupAsync(name);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Групу успішно створено!";
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}