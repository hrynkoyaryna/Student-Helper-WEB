using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Calendar.Models;
using StudentHelper.Application.Calendar.Services;
using StudentHelper.Web.Models.Calendar;

namespace StudentHelper.Web.Controllers;

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
    public async Task<IActionResult> Index()
    {
        const int DemoUserId = 1;

        var events = await this.calendarService.GetUserEventsAsync(DemoUserId);
        return this.View(events);
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
    public async Task<IActionResult> Create(CreatePersonalEventViewModel model)
    {
        const int DemoUserId = 1;

        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        var result = await this.calendarService.CreateEventAsync(new CreatePersonalEventRequest
        {
            UserId = DemoUserId,
            Title = model.Title,
            Description = model.Description,
            Date = model.Date,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
        });

        if (!result.Success)
        {
            this.ModelState.AddModelError(string.Empty, result.ErrorMessage);
            this.logger.LogWarning("Не вдалося створити подію: {Error}", result.ErrorMessage);
            return this.View(model);
        }

        this.TempData["SuccessMessage"] = "Подію успішно створено.";
        return this.RedirectToAction(nameof(this.Index));
    }
}
