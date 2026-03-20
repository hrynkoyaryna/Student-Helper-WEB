using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Tasks;

namespace StudentHelper.Web.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Користувач не авторизований.");
        }

        return int.Parse(userId);
    }

    public async Task<IActionResult> Index(string status = "Поточне", string? subject = null)
    {
        var userId = GetCurrentUserId();

        var model = new TaskIndexViewModel
        {
            SelectedStatus = status,
            SelectedSubject = subject,
            Subjects = await _taskService.GetUserSubjectsAsync(userId),
            Tasks = await _taskService.GetUserTasksAsync(userId, status, subject)
        };

        return View(model);
    }

    public IActionResult Create()
    {
        var model = new TaskCreateEditViewModel
        {
            Deadline = DateTime.Now.AddDays(1),
            Status = "Поточне"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var task = new TaskItem
        {
            Title = model.Title,
            Deadline = DateTime.SpecifyKind(model.Deadline, DateTimeKind.Utc),
            Status = model.Status,
            Subject = model.Subject,
            UserId = GetCurrentUserId()
        };

        await _taskService.CreateTaskAsync(task);

        TempData["SuccessMessage"] = "Завдання успішно створено.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id, GetCurrentUserId());

        if (task == null)
        {
            return NotFound();
        }

        return View(task);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id, GetCurrentUserId());

        if (task == null)
        {
            return NotFound();
        }

        var model = new TaskCreateEditViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Deadline = DateTime.SpecifyKind(task.Deadline, DateTimeKind.Utc),
            Status = task.Status,
            Subject = task.Subject
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TaskCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var task = new TaskItem
        {
            Id = model.Id,
            Title = model.Title,
            Deadline = DateTime.SpecifyKind(model.Deadline, DateTimeKind.Utc),
            Status = model.Status,
            Subject = model.Subject
        };

        var updated = await _taskService.UpdateTaskAsync(task, GetCurrentUserId());

        if (!updated)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Завдання успішно оновлено.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id, GetCurrentUserId());

        if (task == null)
        {
            return NotFound();
        }

        return View(task);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var deleted = await _taskService.DeleteTaskAsync(id, GetCurrentUserId());

        if (!deleted)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Завдання успішно видалено.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, string status)
    {
        var changed = await _taskService.ChangeStatusAsync(id, GetCurrentUserId(), status);

        if (!changed)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Статус завдання змінено.";
        return RedirectToAction(nameof(Index), new { status });
    }
}