using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Web.Models.Calendar;
using StudentHelper.Web.Models.Schedule;

namespace StudentHelper.Web.Controllers.Admin;

[Authorize(Roles = "Admin")]
public class ScheduleController : Controller
{
    private readonly IScheduleService _scheduleService;
    private readonly StudentHelperDbContext _context;
    private readonly ICacheableLookupService _lookupService;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(
        IScheduleService scheduleService,
        StudentHelperDbContext context,
        ICacheableLookupService lookupService,
        ILogger<ScheduleController> logger)
    {
        _scheduleService = scheduleService;
        _context = context;
        _lookupService = lookupService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? groupId, string? weekStartDate)
    {
        var groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
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

        var weekEndDate = startDate.AddDays(6);

        var days = Enumerable.Range(0, 7)
            .Select(i => startDate.AddDays(i))
            .ToList();

        var hours = Enumerable.Range(8, 16)
            .Select(h => new TimeOnly(h, 0))
            .ToList();

        var events = new List<CalendarEventViewModel>();

        if (selectedGroupId > 0)
        {
            var lessons = await _context.ScheduleLessons
                .Include(l => l.Subject)
                .Include(l => l.Teacher)
                .Include(l => l.Group)
                .Where(l => l.GroupId == selectedGroupId)
                .Where(l => DateOnly.FromDateTime(l.Date) >= startDate &&
                            DateOnly.FromDateTime(l.Date) <= weekEndDate)
                .OrderBy(l => l.Date)
                .ThenBy(l => l.StartTime)
                .ToListAsync();

            events.AddRange(lessons.Select(l => new CalendarEventViewModel
            {
                Id = l.Id,
                Title = $"{l.Subject.Title} - {l.Teacher.FullName}",
                Start = l.Date.Date.Add(l.StartTime),
                End = l.Date.Date.Add(l.EndTime),
                Description = l.Place,
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

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateScheduleViewModel
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync(),
            Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync(),
            Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateScheduleViewModel model)
    {
        if ((model.GroupId == null || model.GroupId <= 0) && string.IsNullOrWhiteSpace(model.GroupName))
        {
            ModelState.AddModelError("GroupId", "Потрібно вибрати групу зі списку або ввести нову групу");
        }
        else
        {
            ModelState.Remove("GroupId");
        }

        if ((model.SubjectId == null || model.SubjectId <= 0) && string.IsNullOrWhiteSpace(model.SubjectTitle))
        {
            ModelState.AddModelError("SubjectId", "Потрібно вибрати предмет зі списку або ввести нову назву");
        }
        else
        {
            ModelState.Remove("SubjectId");
        }

        if ((model.TeacherId == null || model.TeacherId <= 0) && string.IsNullOrWhiteSpace(model.TeacherName))
        {
            ModelState.AddModelError("TeacherId", "Потрібно вибрати викладача зі списку або ввести нове ім'я");
        }
        else
        {
            ModelState.Remove("TeacherId");
        }

        if (!ModelState.IsValid)
        {
            model.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
            model.Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
            model.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
            return View(model);
        }

        try
        {
            var groupId = model.GroupId ?? 0;

            if (groupId <= 0 && !string.IsNullOrWhiteSpace(model.GroupName))
            {
                var groupName = model.GroupName.Trim();

                var group = await _context.Groups
                    .FirstOrDefaultAsync(g => g.Name.ToLower() == groupName.ToLower());

                if (group == null)
                {
                    group = new Group { Name = groupName };
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();
                }

                groupId = group.Id;
            }

            if (groupId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Помилка: не вдалось визначити групу");
                model.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
                model.Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
                model.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
                return View(model);
            }

            var subjectId = model.SubjectId ?? 0;

            if (subjectId <= 0 && !string.IsNullOrWhiteSpace(model.SubjectTitle))
            {
                var subjectTitle = model.SubjectTitle.Trim();

                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.Title.ToLower() == subjectTitle.ToLower());

                if (subject == null)
                {
                    subject = new Subject { Title = subjectTitle };
                    _context.Subjects.Add(subject);
                    await _context.SaveChangesAsync();
                }

                subjectId = subject.Id;
            }

            if (subjectId <= 0)
            {
                ModelState.AddModelError("SubjectId", "Потрібно вибрати предмет або ввести його назву");
                model.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
                model.Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
                model.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
                return View(model);
            }

            var teacherId = model.TeacherId ?? 0;

            if (teacherId <= 0 && !string.IsNullOrWhiteSpace(model.TeacherName))
            {
                var teacherName = model.TeacherName.Trim();

                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.FullName.ToLower() == teacherName.ToLower());

                if (teacher == null)
                {
                    teacher = new Teacher { FullName = teacherName };
                    _context.Teachers.Add(teacher);
                    await _context.SaveChangesAsync();
                }

                teacherId = teacher.Id;
            }

            if (teacherId <= 0)
            {
                ModelState.AddModelError("TeacherId", "Потрібно вибрати викладача або ввести його ім'я");
                model.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
                model.Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
                model.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
                return View(model);
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
                GroupId = groupId,
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

                model.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
                model.Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
                model.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();

                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Index), new
            {
                groupId,
                weekStartDate = model.Date.ToString("yyyy-MM-dd")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule lesson");

            ModelState.AddModelError(string.Empty, $"Помилка: {ex.Message}");

            model.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
            model.Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
            model.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();

            return View(model);
        }
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lesson = await _context.ScheduleLessons
            .FirstOrDefaultAsync(s => s.Id == id);

        if (lesson == null)
        {
            return NotFound();
        }

        var groupId = lesson.GroupId;
        var lessonDate = lesson.Date.ToString("yyyy-MM-dd");

        _context.ScheduleLessons.Remove(lesson);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Пару видалено.";

        return RedirectToAction(nameof(Index), new
        {
            groupId,
            weekStartDate = lessonDate
        });
    }

    private static DateOnly GetStartOfWeek(DateOnly date)
    {
        int daysOffset = date.DayOfWeek == DayOfWeek.Sunday
            ? -6
            : -(int)(date.DayOfWeek - DayOfWeek.Monday);

        return date.AddDays(daysOffset);
    }
}