using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        // Отримуємо групи НАПРЯМУ з бази даних, щоб уникнути застарілого кешу
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
        // Запитуємо дані напряму з бази даних, щоб завжди мати найсвіжіший список без кешу
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
        _logger.LogInformation("Create schedule POST called. GroupId={GroupId}, GroupName={GroupName}, Date={Date}", model.GroupId, model.GroupName, model.Date);

        // 1. Ручна перевірка та валідація групи
        if ((model.GroupId == null || model.GroupId <= 0) && string.IsNullOrWhiteSpace(model.GroupName))
        {
            _logger.LogWarning("Create failed: neither GroupId nor GroupName provided");
            ModelState.AddModelError("GroupId", "Потрібно вибрати групу з списку або ввести нову групу");
        }
        else
        {
            ModelState.Remove("GroupId");
        }

        // 2. Ручна перевірка та валідація предмета
        if ((model.SubjectId == null || model.SubjectId <= 0) && string.IsNullOrWhiteSpace(model.SubjectTitle))
        {
            _logger.LogWarning("Create failed: neither SubjectId nor SubjectTitle provided");
            ModelState.AddModelError("SubjectId", "Потрібно вибрати предмет з списку або ввести нову назву");
        }
        else
        {
            ModelState.Remove("SubjectId");
        }

        // 3. Ручна перевірка та валідація викладача
        if ((model.TeacherId == null || model.TeacherId <= 0) && string.IsNullOrWhiteSpace(model.TeacherName))
        {
            _logger.LogWarning("Create failed: neither TeacherId nor TeacherName provided");
            ModelState.AddModelError("TeacherId", "Потрібно вибрати викладача з списку або ввести нове ім'я");
        }
        else
        {
            ModelState.Remove("TeacherId");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid. Errors: {@Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            
            model.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
            model.Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
            model.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
            return View(model);
        }

        try
        {
            // Визначаємо або створюємо групу (з перевіркою на дублікати)
            var groupId = model.GroupId ?? 0;
            if (groupId <= 0 && !string.IsNullOrWhiteSpace(model.GroupName))
            {
                _logger.LogInformation("Creating or finding group: {GroupName}", model.GroupName);
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.Name.ToLower() == model.GroupName.Trim().ToLower());
                if (group != null)
                {
                    groupId = group.Id;
                    _logger.LogInformation("Found existing group: {GroupId}", groupId);
                }
                else
                {
                    group = new Group { Name = model.GroupName.Trim() };
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();
                    groupId = group.Id;
                    _logger.LogInformation("Created new group: {GroupId}", groupId);
                }
            }

            if (groupId <= 0)
            {
                _logger.LogError("GroupId is still invalid: {GroupId}", groupId);
                ModelState.AddModelError(string.Empty, "Помилка: не вдалось визначити групу");
                model.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
                model.Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
                model.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
                return View(model);
            }

            // Визначаємо або створюємо предмет (з перевіркою на дублікати)
            var subjectId = model.SubjectId ?? 0;
            if (subjectId <= 0 && !string.IsNullOrWhiteSpace(model.SubjectTitle))
            {
                _logger.LogInformation("Creating or finding subject: {SubjectTitle}", model.SubjectTitle);
                var subj = await _context.Subjects.FirstOrDefaultAsync(s => s.Title.ToLower() == model.SubjectTitle.Trim().ToLower());
                if (subj != null)
                {
                    subjectId = subj.Id;
                    _logger.LogInformation("Found existing subject: {SubjectId}", subjectId);
                }
                else
                {
                    subj = new Subject { Title = model.SubjectTitle.Trim() };
                    _context.Subjects.Add(subj);
                    await _context.SaveChangesAsync();
                    subjectId = subj.Id;
                    _logger.LogInformation("Created new subject: {SubjectId}", subjectId);
                }
            }

            if (subjectId <= 0)
            {
                _logger.LogError("SubjectId is invalid: {SubjectId}", subjectId);
                ModelState.AddModelError("SubjectId", "Потрібно вибрати предмет або ввести його назву");
                model.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
                model.Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
                model.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
                return View(model);
            }

            // Визначаємо або створюємо викладача (з перевіркою на дублікати)
            var teacherId = model.TeacherId ?? 0;
            if (teacherId <= 0 && !string.IsNullOrWhiteSpace(model.TeacherName))
            {
                _logger.LogInformation("Creating or finding teacher: {TeacherName}", model.TeacherName);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.FullName.ToLower() == model.TeacherName.Trim().ToLower());
                if (teacher != null)
                {
                    teacherId = teacher.Id;
                    _logger.LogInformation("Found existing teacher: {TeacherId}", teacherId);
                }
                else
                {
                    teacher = new Teacher { FullName = model.TeacherName.Trim() };
                    _context.Teachers.Add(teacher);
                    await _context.SaveChangesAsync();
                    teacherId = teacher.Id;
                    _logger.LogInformation("Created new teacher: {TeacherId}", teacherId);
                }
            }

            _logger.LogInformation("Creating schedule lesson: GroupId={GroupId}, SubjectId={SubjectId}, TeacherId={TeacherId}, Date={Date}", groupId, subjectId, teacherId, model.Date);

            var request = new CreateScheduleLessonRequest
            {
                Date = model.Date,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                SubjectId = subjectId,
                SubjectTitle = model.SubjectTitle,
                TeacherId = teacherId > 0 ? teacherId : null,
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
                _logger.LogError("Failed to create schedule lesson: {Message}", result.Message);
                ModelState.AddModelError(string.Empty, result.Message);
                TempData["ErrorMessage"] = result.Message;
                model.Groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
                model.Subjects = await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
                model.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
                return View(model);
            }

            _logger.LogInformation("Schedule lesson created successfully");
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index", new { groupId = groupId, weekStartDate = model.Date.ToString("yyyy-MM-dd") });
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
}