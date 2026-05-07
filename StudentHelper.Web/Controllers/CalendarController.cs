using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Models.Calendar;
using StudentHelper.Application.Services;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Web.Models.Calendar;

namespace StudentHelper.Web.Controllers;

public class CalendarController : BaseController
{
    private readonly ICalendarService _calendarService;
    private readonly ITaskService _taskService;
    private readonly IExamsService _examsService;
    private readonly IUserService _userService;
    private readonly StudentHelperDbContext _context;
    private readonly IOptions<ApplicationSettings> _settings;

    public CalendarController(
        ICalendarService calendarService,
        ITaskService taskService,
        IExamsService examsService,
        IUserService userService,
        StudentHelperDbContext context,
        IOptions<ApplicationSettings> settings)
    {
        _calendarService = calendarService;
        _taskService = taskService;
        _examsService = examsService;
        _userService = userService;
        _context = context;
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? weekStartDate, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var startDate = !string.IsNullOrWhiteSpace(weekStartDate) && DateOnly.TryParse(weekStartDate, out var parsedDate)
            ? GetStartOfWeek(parsedDate)
            : GetStartOfWeek(today);

        var weekEndDate = startDate.AddDays(6);

        var weekStartDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var weekEndDateTime = weekEndDate.ToDateTime(TimeOnly.MaxValue);

        var days = Enumerable.Range(0, 7)
            .Select(i => startDate.AddDays(i))
            .ToList();

        var calendarStartHour = _settings.Value.CalendarStartHour;
        var hoursCount = 24 - calendarStartHour;

        var timeSlots = Enumerable.Range(calendarStartHour, hoursCount)
            .Select(hour => new TimeOnly(hour, 0))
            .ToList();

        var events = new List<CalendarEventViewModel>();

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

        var tasksResult = await _taskService.GetAllUserTasksAsync(userId);
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

        var exams = await _examsService.GetByUserIdAsync(userId);
        events.AddRange(exams.Select(e => new CalendarEventViewModel
        {
            Id = e.Id,
            Title = $"{e.Subject} - {e.TeacherName}",
            Start = e.DateTime,
            End = e.DateTime.AddHours(2),
            Description = e.Description,
            Color = "#dc3545",
            Type = "Exam"
        }));

        var user = await _userService.GetUserByIdAsync(userId);

        if (user?.GroupId.HasValue == true)
        {
            var groupExams = await _examsService.GetByGroupIdAsync(user.GroupId.Value);
            events.AddRange(groupExams.Select(e => new CalendarEventViewModel
            {
                Id = e.Id,
                Title = $"{e.Subject} - {e.TeacherName}",
                Start = e.DateTime,
                End = e.DateTime.AddHours(2),
                Description = e.Description,
                Color = "#dc3545",
                Type = "Exam"
            }));
        }

        IQueryable<StudentHelper.Domain.Entities.ScheduleLesson> lessonsQuery = _context.ScheduleLessons
            .Include(l => l.Subject)
            .Include(l => l.Teacher)
            .Include(l => l.Group)
            .Where(l => l.Date >= weekStartDateTime && l.Date <= weekEndDateTime);

        if (!User.IsInRole("Admin"))
        {
            if (user?.GroupId.HasValue != true)
            {
                lessonsQuery = lessonsQuery.Where(l => false);
            }
            else
            {
                lessonsQuery = lessonsQuery.Where(l => l.GroupId == user.GroupId.Value);
            }
        }

        var lessons = await lessonsQuery.ToListAsync(cancellationToken);

        events.AddRange(lessons.Select(l => new CalendarEventViewModel
        {
            Id = l.Id,
            Title = User.IsInRole("Admin")
                ? $"{l.Group.Name}: {l.Subject.Title} - {l.Teacher.FullName}"
                : $"{l.Subject.Title} - {l.Teacher.FullName}",
            Start = l.Date.Date.Add(l.StartTime),
            End = l.Date.Date.Add(l.EndTime),
            Description = l.Place,
            Color = l.Type == "Lecture" ? "#0d6efd" : "#6610f2",
            Type = "Lesson"
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
        int daysOffset = date.DayOfWeek == DayOfWeek.Sunday
            ? -6
            : -(int)(date.DayOfWeek - DayOfWeek.Monday);

        return date.AddDays(daysOffset);
    }
}