using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Events;

namespace StudentHelper.Web.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly StudentHelperDbContext _context;

    public EventsController(StudentHelperDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        
        var events = await _context.PersonalEvents
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.StartTime)
            .ToListAsync();

        return View(events);
    }

    public IActionResult Create()
    {
        var model = new EventCreateViewModel
        {
            EventDate = DateTime.Today
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var newEvent = new PersonalEvent
        {
            Title = model.Title,
            Description = $"[Дата: {model.EventDate:dd.MM.yyyy}] {model.Description}",
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            UserId = GetCurrentUserId()
        };

        _context.PersonalEvents.Add(newEvent);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Подію успішно створено.";
        return RedirectToAction("Index", "Calendar");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetCurrentUserId();
        var ev = await _context.PersonalEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (ev == null)
        {
            return NotFound();
        }

        var model = new EventCreateViewModel
        {
            Title = ev.Title,
            Description = ev.Description,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
            EventDate = DateTime.Today 
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        var ev = await _context.PersonalEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (ev == null)
        {
            return NotFound();
        }

        ev.Title = model.Title;
        ev.Description = $"[Дата: {model.EventDate:dd.MM.yyyy}] {model.Description}";
        ev.StartTime = model.StartTime;
        ev.EndTime = model.EndTime;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Подію оновлено.";
        return RedirectToAction("Index", "Calendar");
    }

    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var ev = await _context.PersonalEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (ev == null)
        {
            return NotFound();
        }

        return View(ev);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = GetCurrentUserId();
        var ev = await _context.PersonalEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (ev != null)
        {
            _context.PersonalEvents.Remove(ev);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Подію видалено.";
        }

        return RedirectToAction("Index", "Calendar");
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Користувач не авторизований.");
        }
        return int.Parse(userId);
    }
}