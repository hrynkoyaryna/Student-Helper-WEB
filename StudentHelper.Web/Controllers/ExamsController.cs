using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces;
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
        var exams = await _examsService.GetExamsAsync();

        // subjects list
        var subjects = exams.Select(e => e.Subject).Distinct().OrderBy(s => s).ToList();

        // time filtering
        var nowUtc = DateTime.UtcNow;
        if (time == "past")
        {
            exams = exams.Where(e => e.DateTime < nowUtc).ToList();
        }
        else if (time == "upcoming")
        {
            exams = exams.Where(e => e.DateTime >= nowUtc).ToList();
        }

        // subject filtering
        if (!string.IsNullOrWhiteSpace(subject))
        {
            exams = exams.Where(e => e.Subject == subject).ToList();
        }

        // sort by subject
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

    public async Task<IActionResult> Create()
    {
        var teachers = await _examsService.GetAllTeachersAsync();
        var model = new ExamCreateEditViewModel { DateTime = DateTime.Now.AddDays(7), Teachers = teachers };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExamCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Teachers = await _examsService.GetAllTeachersAsync();
            return View(model);
        }

        var request = new CreateExamRequest
        {
            Subject = model.Subject,
            DateTime = model.DateTime,
            TeacherId = model.TeacherId,
            TeacherName = model.TeacherName
        };

        var result = await _examsService.CreateExamAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            model.Teachers = await _examsService.GetAllTeachersAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var exam = await _examsService.GetExamByIdAsync(id);
        if (exam == null) return NotFound();

        var teachers = await _examsService.GetAllTeachersAsync();

        var model = new ExamCreateEditViewModel
        {
            Id = exam.Id,
            Subject = exam.Subject,
            DateTime = DateTime.SpecifyKind(exam.DateTime, DateTimeKind.Local),
            TeacherId = exam.TeacherId,
            Teachers = teachers
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ExamCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Teachers = await _examsService.GetAllTeachersAsync();
            return View(model);
        }

        var request = new UpdateExamRequest
        {
            Id = model.Id,
            Subject = model.Subject,
            DateTime = model.DateTime,
            TeacherId = model.TeacherId,
            TeacherName = model.TeacherName
        };

        var result = await _examsService.UpdateExamAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            model.Teachers = await _examsService.GetAllTeachersAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var exam = await _examsService.GetExamByIdAsync(id);
        if (exam == null) return NotFound();
        return View(exam);
    }

    [HttpPost, ActionName("Delete")]
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
