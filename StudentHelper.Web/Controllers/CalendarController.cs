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

    public CalendarController(ICalendarService calendarService)
    {
        this.calendarService = calendarService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? weekStartDate, CancellationToken cancellationToken)
    {
        var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId)) return this.Unauthorized();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = weekStartDate ?? GetStartOfWeek(today);

        var days = Enumerable.Range(0, 7).Select(offset => startDate.AddDays(offset)).ToList();
        var timeSlots = Enumerable.Range(7, 16).Select(hour => new TimeOnly(hour, 0)).ToList();

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
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId)) return this.Unauthorized();

        var personalEvent = await this.calendarService.GetEventByIdAsync(id, cancellationToken);
        if (personalEvent == null || personalEvent.UserId != userId) return this.NotFound();

        return this.View(personalEvent);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return this.View(new CreatePersonalEventViewModel
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
        if (!this.ModelState.IsValid) return this.View(model);

        var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId)) return this.Unauthorized();

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

        var result = await this.calendarService.CreateEventAsync(request, cancellationToken);
        if (!result.Success)
        {
            this.ModelState.AddModelError(string.Empty, result.Message);
            return this.View(model);
        }

        TempData["CalendarMessage"] = result.Message;
        return this.RedirectToAction(nameof(this.Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId)) return this.Unauthorized();

        var personalEvent = await this.calendarService.GetEventByIdAsync(id, cancellationToken);
        if (personalEvent == null || personalEvent.UserId != userId) return this.NotFound();

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

        return this.View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditPersonalEventViewModel model, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid) return this.View(model);

        var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId)) return this.Unauthorized();

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

        var result = await this.calendarService.UpdateEventAsync(request, cancellationToken);
        if (!result.Success)
        {
            this.ModelState.AddModelError(string.Empty, result.Message);
            return this.View(model);
        }

        TempData["CalendarMessage"] = result.Message;
        return this.RedirectToAction(nameof(this.Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId)) return this.Unauthorized();

        var result = await this.calendarService.DeleteEventAsync(id, userId, cancellationToken);
        if (!result.Success) return this.RedirectToAction(nameof(this.Details), new { id });

        return this.RedirectToAction(nameof(this.Index));
    }

    private static DateOnly GetStartOfWeek(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var diff = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.AddDays(-diff);
    }
}