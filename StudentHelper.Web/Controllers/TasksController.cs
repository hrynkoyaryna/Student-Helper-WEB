using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Tasks;

namespace StudentHelper.Web.Controllers;

public class TasksController : BaseController
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;
    private readonly IOptions<ApplicationSettings> _settings;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger, IOptions<ApplicationSettings> settings)
    {
        _taskService = taskService;
        _logger = logger;
        _settings = settings;
    }

    public async Task<IActionResult> Index(
        string status = "Поточне",
        string? subject = null,
        string? searchTerm = null,
        int page = 1)
    {
        var userId = GetCurrentUserId();
        
        // Перевірка мінімальної довжини пошуку
        if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Length < _settings.Value.MinSearchCharacters)
        {
            TempData["WarningMessage"] = $"Пошук повинен мати щонайменше {_settings.Value.MinSearchCharacters} символів. Показуються всі завдання.";
            searchTerm = null; // Очищаємо пошук
        }

        // Валідація номера сторінки
        if (page < 1) page = 1;

        var subjectsResult = await _taskService.GetUserSubjectsAsync(userId);
        var tasksResult = await _taskService.GetUserTasksAsync(userId, status, subject, searchTerm, page);
        
        // Отримуємо загальну кількість завдань для розрахунку пагінації
        var totalCount = await _taskService.GetUserTasksCountAsync(userId, status, subject, searchTerm);

        if (!subjectsResult.Success || !tasksResult.Success)
        {
            return BadRequest("Не вдалося завантажити список завдань.");
        }

        var itemsPerPage = _settings.Value.ItemsPerPage;
        var totalPages = (int)Math.Ceiling((double)totalCount / itemsPerPage);
        
        // Валідація - якщо сторінка більше за максимальну, перенаправляємо на останню
        if (page > totalPages && totalPages > 0)
        {
            return RedirectToAction(nameof(Index), new { status, subject, searchTerm, page = totalPages });
        }

        var model = new TaskIndexViewModel
        {
            SelectedStatus = status,
            SelectedSubject = subject,
            SearchTerm = searchTerm,
            Subjects = subjectsResult.Value ?? new List<string>(),
            Tasks = tasksResult.Value ?? new List<TaskItem>(),
            CurrentPage = page,
            TotalItems = totalCount,
            ItemsPerPage = itemsPerPage,
            TotalPages = totalPages
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

        var statusMap = new Dictionary<string, string>
        {
            { "ToDo", "Поточне" },
            { "InProgress", "Поточне" },
            { "Done", "Виконане" }
        };

        var task = new TaskItem
        {
            Title = model.Title,
            Deadline = DateTime.SpecifyKind(model.Deadline, DateTimeKind.Local).ToUniversalTime(),
            Status = statusMap.ContainsKey(model.Status) ? statusMap[model.Status] : "Поточне",
            Subject = model.Subject,
            Description = model.Description,
            UserId = GetCurrentUserId()
        };

        var result = await _taskService.CreateTaskAsync(task);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await _taskService.GetTaskByIdAsync(id, GetCurrentUserId());

        if (!result.Success)
        {
            return NotFound();
        }

        return View(result.Value);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var result = await _taskService.GetTaskByIdAsync(id, GetCurrentUserId());

        if (!result.Success)
        {
            return NotFound();
        }

        var task = result.Value;

        var model = new TaskCreateEditViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Deadline = DateTime.SpecifyKind(task.Deadline, DateTimeKind.Utc),
            Status = task.Status,
            Subject = task.Subject,
            Description = task.Description
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
            Deadline = DateTime.SpecifyKind(model.Deadline, DateTimeKind.Local).ToUniversalTime(),
            Status = model.Status,
            Subject = model.Subject,
            Description = model.Description
        };

        var updated = await _taskService.UpdateTaskAsync(task, GetCurrentUserId());

        if (!updated.Success)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = updated.Message;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var result = await _taskService.GetTaskByIdAsync(id, GetCurrentUserId());

        if (!result.Success)
        {
            return NotFound();
        }

        return View(result.Value);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var deleted = await _taskService.DeleteTaskAsync(id, GetCurrentUserId());

        if (!deleted.Success)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = deleted.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, string status)
    {
        var changed = await _taskService.ChangeStatusAsync(id, GetCurrentUserId(), status);

        if (!changed.Success)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = changed.Message;
        return RedirectToAction(nameof(Index), new { status });
    }
}