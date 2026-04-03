using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces; 
using StudentHelper.Application.Models.Calendar;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Calendar;

namespace StudentHelper.Web.Controllers;

public class CalendarController : BaseController
{
    private readonly ICalendarService _calendarService;
    private readonly ITaskService _taskService;
    private readonly IExamsService _examsService;

    public CalendarController(ICalendarService calendarService, ITaskService taskService, IExamsService examsService)
    {
        _calendarService = calendarService;
        _taskService = taskService;
        _examsService = examsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? weekStartDate, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var today = DateOnly.FromDateTime(DateTime.Today);
        
        // Парсимо дату з URL параметра або використовуємо сьогоднішню дату
        DateOnly startDate;
        if (!string.IsNullOrWhiteSpace(weekStartDate) && DateOnly.TryParse(weekStartDate, out var parsedDate))
        {
            startDate = GetStartOfWeek(parsedDate);
        }
        else
        {
            startDate = GetStartOfWeek(today);
        }

        // Генеруємо 7 днів починаючи з понеділка
        var days = new List<DateOnly>();
        for (int i = 0; i < 7; i++)
        {
            days.Add(startDate.AddDays(i));
        }
        
        var timeSlots = Enumerable.Range(7, 16).Select(hour => new TimeOnly(hour, 0)).ToList();

        var events = new List<CalendarEventViewModel>();

        // Додаємо особисті eventos
        var allPersonalEvents = await _calendarService.GetFullCalendarDataAsync(userId, cancellationToken);
        events.AddRange(allPersonalEvents.Select(e => new CalendarEventViewModel
        {
            Id = e.Id,
            Title = e.Title,
            Start = e.Start,
            End = e.End,
            Description = e.Description,
            Color = e.Color ?? "#007bff",
            Type = "Event"
        }));

        // Додаємо завдання
        var tasksResult = await _taskService.GetUserTasksAsync(userId);
        if (!tasksResult.Success || tasksResult.Value is null)
        {
            return BadRequest(tasksResult.Message);
        }

        events.AddRange(tasksResult.Value.Select(t => new CalendarEventViewModel
        {
            Id = t.Id,
            Title = t.Title,
            Start = t.Deadline,
            End = t.Deadline.AddHours(1),
            Description = t.Description,
            Color = "#ffc107",
            Type = "Task"
        }));

        // Додаємо екзамени
        var exams = await _examsService.GetByUserIdAsync(userId);
        events.AddRange(exams.Select(e => new CalendarEventViewModel
        {
            Id = e.Id,
            Title = $"{e.Subject} - {e.TeacherName}",
            Start = e.DateTime,
            End = e.DateTime.AddHours(2),
            Description = e.Description,
            Color = "#dc3545", // червоний для екзаменів
            Type = "Exam"
        }));

        var model = new CalendarIndexViewModel
        {
            WeekStartDate = startDate,
            Days = days,
            TimeSlots = timeSlots,
            Events = events
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var personalEvent = await _calendarService.GetEventByIdAsync(id, cancellationToken);
        if (personalEvent == null || personalEvent.UserId != userId) return NotFound();

        return View(personalEvent);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreatePersonalEventViewModel
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Color = "#5bc8d8" 
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePersonalEventViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = GetCurrentUserId();

        var request = new CreatePersonalEventRequest
        {
            UserId = userId,
            Title = model.Title,
            Description = model.Description,
            Date = model.Date,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            Color = model.Color ?? "#5bc8d8"
        };

        var result = await _calendarService.CreateEventAsync(request, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["CalendarMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var personalEvent = await _calendarService.GetEventByIdAsync(id, cancellationToken);
        if (personalEvent == null || personalEvent.UserId != userId) return NotFound();

        var model = new EditPersonalEventViewModel
        {
            Id = personalEvent.Id,
            Title = personalEvent.Title,
            Description = personalEvent.Description,
            Date = DateOnly.FromDateTime(personalEvent.StartAt),
            StartTime = TimeOnly.FromDateTime(personalEvent.StartAt),
            EndTime = TimeOnly.FromDateTime(personalEvent.EndAt),
            Color = string.IsNullOrEmpty(personalEvent.Color) ? "#5bc8d8" : personalEvent.Color
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditPersonalEventViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = GetCurrentUserId();

        var request = new EditPersonalEventRequest
        {
            Id = model.Id,
            UserId = userId,
            Title = model.Title,
            Description = model.Description,
            Date = model.Date,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            Color = model.Color ?? "#5bc8d8"
        };

        var result = await _calendarService.UpdateEventAsync(request, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["CalendarMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _calendarService.DeleteEventAsync(id, userId, cancellationToken);
        if (!result.Success) return RedirectToAction(nameof(Details), new { id });

        return RedirectToAction(nameof(Index));
    }

    private static DateOnly GetStartOfWeek(DateOnly date)
    {
        // Переводимо в понеділок (DayOfWeek.Monday = 1)
        // Якщо неділя (0), переходимо назад на 6 днів
        // Інакше переходимо на (DayOfWeek - 1) днів назад
        int daysOffset = date.DayOfWeek == DayOfWeek.Sunday ? -6 : -(int)(date.DayOfWeek - DayOfWeek.Monday);
        return date.AddDays(daysOffset);
    }
}