using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Notes;

namespace StudentHelper.Web.Controllers;

[Authorize]
public class NotesController : Controller
{
    private readonly INotesService _notesService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(INotesService notesService, ILogger<NotesController> logger)
    {
        _notesService = notesService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Користувач не аутентифікований.");
        }

        return int.Parse(userId);
    }

    public async Task<IActionResult> Index(string? search = null)
    {
        var userId = GetCurrentUserId();
        var notes = await _notesService.GetUserNotesAsync(userId, search);

        var model = new NotesIndexViewModel
        {
            Notes = notes,
            SearchQuery = search
        };

        return View(model);
    }

    public IActionResult Create()
    {
        return View(new NoteCreateEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NoteCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var note = new Note
        {
            Title = model.Title,
            Body = model.Body,
            UserId = GetCurrentUserId(),
            Pinned = false
        };

        await _notesService.CreateNoteAsync(note);

        TempData["SuccessMessage"] = "Нотатка підтримати успішно створена.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var note = await _notesService.GetNoteByIdAsync(id, GetCurrentUserId());

        if (note == null)
        {
            return NotFound();
        }

        var model = new NoteCreateEditViewModel
        {
            Title = note.Title,
            Body = note.Body
        };

        ViewBag.NoteId = id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NoteCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.NoteId = id;
            return View(model);
        }

        var note = new Note
        {
            Id = id,
            Title = model.Title,
            Body = model.Body
        };

        var success = await _notesService.UpdateNoteAsync(note, GetCurrentUserId());

        if (!success)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Нотатка успішно оновлена.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _notesService.DeleteNoteAsync(id, GetCurrentUserId());

        if (!success)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Нотатка успішно видалена.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pin(int id)
    {
        var success = await _notesService.PinNoteAsync(id, GetCurrentUserId());

        if (!success)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpin(int id)
    {
        var success = await _notesService.UnpinNoteAsync(id, GetCurrentUserId());

        if (!success)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}
