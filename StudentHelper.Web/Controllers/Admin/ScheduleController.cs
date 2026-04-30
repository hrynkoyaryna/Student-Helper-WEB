using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Web.Models.Schedule;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Calendar;

namespace StudentHelper.Web.Controllers.Admin;

[Authorize(Roles = "Admin")]
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

    [HttpGet]
    public async Task<IActionResult> Index(int? groupId, string? weekStartDate)
    {
        var groups = await _lookupService.GetAllGroupsAsync();
        var selectedGroupId = groupId ?? (groups.FirstOrDefault()?.Id ?? 0);

        DateOnly startDate;
        if (!string.IsNullOrWhiteSpace(weekStartDate) && DateOnly.TryParse(weekStartDate, out var parsed))
        {
            startDate = GetStartOfWeek(parsed);
        }
        else
        {
            startDate = GetStartOfWeek(DateOnly.FromDateTime(DateTime.Today));
        }

        var days = Enumerable.Range(0, 7).Select(i => startDate.AddDays(i)).ToList();
        var hours = Enumerable.Range(8, 16).Select(h => new TimeOnly(h, 0)).ToList();

        var events = new List<CalendarEventViewModel>();

        if (selectedGroupId > 0)
        {
            var lessons = await _scheduleService.GetScheduleByGroupIdAsync(selectedGroupId);
            events.AddRange(lessons.Select(l => new CalendarEventViewModel
            {
                Id = l.Id,
                Title = $"{l.Subject.Title} - {l.Teacher.FullName}",
                Start = l.Date.Add(l.StartTime),
                End = l.Date.Add(l.EndTime),
                Color = l.Type == "Lecture" ? "#0d6efd" : "#6610f2",
                Type = "Lesson"
            }));
        }

        var model = new GroupScheduleViewModel
        {
            Groups = groups,
            SelectedGroupId = selectedGroupId,
            WeekStartDate = startDate,
            Days = days,
            TimeSlots = hours,
            Events = events
        };

        return View(model);
    }

    private static DateOnly GetStartOfWeek(DateOnly date)
    {
        int daysOffset = date.DayOfWeek == DayOfWeek.Sunday ? -6 : -(int)(date.DayOfWeek - DayOfWeek.Monday);
        return date.AddDays(daysOffset);
    }

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
            RecurrenceType = model.RecurrenceType,
            RecurrenceUntil = model.RecurrenceUntil,
            Place = model.Place
        };

        var result = await _scheduleService.CreateScheduleLessonAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            TempData["ErrorMessage"] = result.Message;
            model.Groups = await _lookupService.GetAllGroupsAsync();
            model.Subjects = await _lookupService.GetAllSubjectsAsync();
            model.Teachers = await _lookupService.GetAllTeachersAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        // Redirect to admin schedule index for the group so admin can see the created lessons
        return RedirectToAction("Index", new { groupId = model.GroupId, weekStartDate = model.Date.ToString("yyyy-MM-dd") });
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
