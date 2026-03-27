using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Models.Calendar;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Calendar;

namespace StudentHelper.Web.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly ICalendarService calendarService;
    private readonly ILogger<CalendarController> logger;

    public CalendarController(
        ICalendarService calendarService,
        ILogger<CalendarController> logger)
    {
        this.calendarService = calendarService;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? weekStartDate, CancellationToken cancellationToken)
    {
        var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return this.Unauthorized();
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = weekStartDate ?? GetStartOfWeek(today);

        var days = Enumerable.Range(0, 7)
            .Select(offset => startDate.AddDays(offset))
            .ToList();

        var timeSlots = Enumerable.Range(8, 15)
            .Select(hour => new TimeOnly(hour, 0))
            .ToList();

        var events = await this.calendarService.GetUserEventsAsync(userId, cancellationToken);

        var model = new CalendarIndexViewModel
        {
            WeekStartDate = startDate,
            Days = days,
            TimeSlots = timeSlots,
            Events = events,
        };

        return this.View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new CreatePersonalEventViewModel
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
        };

        return this.View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreatePersonalEventViewModel model,
        CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return this.Unauthorized();
        }

        var request = new CreatePersonalEventRequest
        {
            UserId = userId,
            Title = model.Title,
            Description = model.Description,
            Date = model.Date,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
        };

        var result = await this.calendarService.CreateEventAsync(request, cancellationToken);

        if (!result.Success)
        {
            this.ModelState.AddModelError(string.Empty, result.Message);
            return this.View(model);
        }

        this.logger.LogInformation("���������� {UserId} ������� ���� {Title}", userId, model.Title);

        return this.RedirectToAction(nameof(this.Index));
    }

    private static DateOnly GetStartOfWeek(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var diff = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.AddDays(-diff);
    }
}