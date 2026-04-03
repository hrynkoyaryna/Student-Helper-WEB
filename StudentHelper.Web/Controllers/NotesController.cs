using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Notes;

namespace StudentHelper.Web.Controllers;

public class NotesController : BaseController
{
    private readonly INotesService _notesService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(INotesService notesService, ILogger<NotesController> logger)
    {
        _notesService = notesService;
        _logger = logger;
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

        var result = await _notesService.CreateNoteAsync(note);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
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

        var result = await _notesService.UpdateNoteAsync(note, GetCurrentUserId());

        if (!result.Success)
        {
            // show error to user on the same page
            ModelState.AddModelError("", result.Message);
            ViewBag.NoteId = id;
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _notesService.DeleteNoteAsync(id, GetCurrentUserId());

        if (!result.Success)
        {
            // Redirect to index with error message so user sees it
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pin(int id)
    {
        var result = await _notesService.PinNoteAsync(id, GetCurrentUserId());

        if (!result.Success)
        {
            // If AJAX, return bad request with error message so frontend can show it
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(result.Message);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpin(int id)
    {
        var result = await _notesService.UnpinNoteAsync(id, GetCurrentUserId());

        if (!result.Success)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(result.Message);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }
}
