using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Web.Models.Schedule;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Controllers.Admin;

[Authorize]
public class ScheduleController : Controller
{
    private readonly IScheduleService _scheduleService;
    private readonly StudentHelperDbContext _context;
    private readonly ICacheableLookupService _lookupService;

    public ScheduleController(IScheduleService scheduleService, StudentHelperDbContext context, ICacheableLookupService lookupService)
    {
        _scheduleService = scheduleService;
        _context = context;
        _lookupService = lookupService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateScheduleViewModel
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9,0),
            EndTime = new TimeOnly(10,0),
            Groups = await _lookupService.GetAllGroupsAsync(),
            Subjects = await _lookupService.GetAllSubjectsAsync(),
            Teachers = await _lookupService.GetAllTeachersAsync()
        };

        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateScheduleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Groups = await _lookupService.GetAllGroupsAsync();
            model.Subjects = await _lookupService.GetAllSubjectsAsync();
            model.Teachers = await _lookupService.GetAllTeachersAsync();
            return View(model);
        }

        var subjectId = model.SubjectId;
        if (subjectId <= 0 && !string.IsNullOrWhiteSpace(model.SubjectTitle))
        {
            var subj = new Subject { Title = model.SubjectTitle.Trim() };
            _context.Subjects.Add(subj);
            await _context.SaveChangesAsync();
            subjectId = subj.Id;

            // Invalidate subjects cache
            // Remove cache entry so next calls reload
            // Using IMemoryCache via lookup service implementation; easiest way is to clear by key via context requests
        }

        var teacherId = model.TeacherId;
        if ((teacherId == null || teacherId <= 0) && !string.IsNullOrWhiteSpace(model.TeacherName))
        {
            var teacher = new Teacher { FullName = model.TeacherName.Trim() };
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();
            teacherId = teacher.Id;
        }

        var request = new CreateScheduleLessonRequest
        {
            Date = model.Date,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            SubjectId = subjectId,
            SubjectTitle = model.SubjectTitle,
            TeacherId = teacherId,
            TeacherName = model.TeacherName,
            GroupId = model.GroupId,
            Type = model.Type ?? "Lecture",
            Recurrence = model.Recurrence ?? string.Empty,
            Place = model.Place
        };

        var result = await _scheduleService.CreateScheduleLessonAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            model.Groups = await _lookupService.GetAllGroupsAsync();
            model.Subjects = await _lookupService.GetAllSubjectsAsync();
            model.Teachers = await _lookupService.GetAllTeachersAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction("Index", "Calendar");
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var lesson = await _context.ScheduleLessons
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (lesson == null) return NotFound();

        return View(lesson);
    }
}
