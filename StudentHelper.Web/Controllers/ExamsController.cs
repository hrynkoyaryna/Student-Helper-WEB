using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Exams;

namespace StudentHelper.Web.Controllers;

[Authorize]
public class ExamsController : Controller
{
    private readonly IExamsService _examsService;
    private readonly ILogger<ExamsController> _logger;

    public ExamsController(IExamsService examsService, ILogger<ExamsController> logger)
    {
        _examsService = examsService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? subject = null, string time = "all", string sort = "subject_asc")
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) !);
        var exams = (await _examsService.GetByUserIdAsync(userId)).ToList();

        var subjects = exams.Select(e => e.Subject).Distinct().OrderBy(s => s).ToList();

        var nowUtc = DateTime.UtcNow;
        if (time == "past")
        {
            exams = exams.Where(e => e.DateTime < nowUtc).ToList();
        }
        else if (time == "upcoming")
        {
            exams = exams.Where(e => e.DateTime >= nowUtc).ToList();
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            exams = exams.Where(e => e.Subject == subject).ToList();
        }

        exams = sort switch
        {
            "subject_desc" => exams.OrderByDescending(e => e.Subject).ToList(),
            _ => exams.OrderBy(e => e.Subject).ToList()
        };

        var model = new ExamIndexViewModel
        {
            Exams = exams,
            Subjects = subjects,
            SelectedSubject = subject,
            TimeFilter = time,
            SortOrder = sort
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var exam = await _examsService.GetExamByIdAsync(id);
        if (exam == null) return NotFound();
        return View(exam);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        var model = new ExamCreateEditViewModel 
        { 
            DateTime = DateTime.Now.AddDays(7) 
        };
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExamCreateEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) !);

        var request = new CreateExamRequest
        {
            Subject = model.Subject,
            DateTime = model.DateTime,
            TeacherName = model.TeacherName,
            Description = model.Description, 
            UserId = userId
        };

        var result = await _examsService.CreateExamAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var exam = await _examsService.GetExamByIdAsync(id);
        if (exam == null) return NotFound();

        var model = new ExamCreateEditViewModel
        {
            Id = exam.Id,
            Subject = exam.Subject,
            DateTime = DateTime.SpecifyKind(exam.DateTime, DateTimeKind.Local),
            TeacherName = exam.TeacherName,
            Description = exam.Description
        };

        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ExamCreateEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) !);

        var request = new UpdateExamRequest
        {
            Id = model.Id,
            Subject = model.Subject,
            DateTime = model.DateTime,
            TeacherName = model.TeacherName,
            Description = model.Description, 
            UserId = userId
        };

        var result = await _examsService.UpdateExamAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var exam = await _examsService.GetExamByIdAsync(id);
        if (exam == null) return NotFound();
        return View(exam);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _examsService.DeleteExamAsync(id);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}