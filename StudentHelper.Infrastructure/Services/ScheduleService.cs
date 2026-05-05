using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Services;

public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _repository;
    private readonly StudentHelperDbContext _context;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(
        IScheduleRepository repository,
        StudentHelperDbContext context,
        ILogger<ScheduleService> logger)
    {
        _repository = repository;
        _context = context;
        _logger = logger;
    }

    public async Task<Result> CreateScheduleLessonAsync(CreateScheduleLessonRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Date == default) return Result.Fail("Вкажіть дату");
            if (request.SubjectId <= 0) return Result.Fail("Вкажіть предмет");
            if (request.TeacherId == null || request.TeacherId <= 0) return Result.Fail("Вкажіть викладача");
            if (request.GroupId <= 0) return Result.Fail("Вкажіть групу");
            if (request.StartTime >= request.EndTime) return Result.Fail("Час початку має бути раніше за час закінчення");

            var groupExists = await _context.Groups.AnyAsync(g => g.Id == request.GroupId, cancellationToken);
            if (!groupExists) return Result.Fail("Групу не знайдено");

            var teacherExists = await _context.Teachers.AnyAsync(t => t.Id == request.TeacherId, cancellationToken);
            if (!teacherExists) return Result.Fail("Викладача не знайдено");

            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId, cancellationToken);
            if (!subjectExists) return Result.Fail("Предмет не знайдено");

            // Build occurrences based on recurrence type
            var occurrences = new List<DateOnly>();
            var start = request.Date;
            occurrences.Add(start);

            if (!string.IsNullOrWhiteSpace(request.RecurrenceType) && request.RecurrenceType != "None")
            {
                var until = request.RecurrenceUntil ?? request.Date.AddDays(7 * 16); // default ~ semester

                Func<DateOnly, DateOnly> step = request.RecurrenceType switch
                {
                    "Daily" => d => d.AddDays(1),
                    "Weekly" => d => d.AddDays(7),
                    "BiWeekly" => d => d.AddDays(14),
                    _ => d => d.AddDays(7),
                };

                var next = step(start);
                var safety = 0;
                while (next <= until && safety < 1000)
                {
                    occurrences.Add(next);
                    next = step(next);
                    safety++;
                }
            }

            var startTs = request.StartTime.ToTimeSpan();
            var endTs = request.EndTime.ToTimeSpan();

            // conflict checks
            foreach (var occ in occurrences)
            {
                var exLessons = await _context.ScheduleLessons
                    .Where(s => s.Date.Year == occ.Year && s.Date.Month == occ.Month && s.Date.Day == occ.Day)
                    .Where(s => s.GroupId == request.GroupId || s.TeacherId == request.TeacherId || (!string.IsNullOrWhiteSpace(request.Place) && s.Place == request.Place))
                    .ToListAsync(cancellationToken);

                foreach (var ex in exLessons)
                {
                    if (startTs < ex.EndTime && endTs > ex.StartTime)
                    {
                        if (ex.GroupId == request.GroupId)
                        {
                            return Result.Fail($"Конфлікт: у групи вже є пара {ex.StartTime} - {ex.EndTime} на {occ:yyyy-MM-dd}");
                        }
                        if (ex.TeacherId == request.TeacherId)
                        {
                            return Result.Fail($"Конфлікт: у викладача вже є пара {ex.StartTime} - {ex.EndTime} на {occ:yyyy-MM-dd}");
                        }
                        if (!string.IsNullOrWhiteSpace(request.Place) && ex.Place == request.Place)
                        {
                            return Result.Fail($"Конфлікт: аудиторія '{request.Place}' вже зайнята {ex.StartTime} - {ex.EndTime} на {occ:yyyy-MM-dd}");
                        }
                    }
                }
            }

            foreach (var occ in occurrences)
            {
                var lesson = new ScheduleLesson
                {
                    Date = occ.ToDateTime(new TimeOnly(0, 0)),
                    StartTime = startTs,
                    EndTime = endTs,
                    SubjectId = request.SubjectId,
                    TeacherId = request.TeacherId!.Value,
                    GroupId = request.GroupId,
                    Type = request.Type ?? "Lecture",
                    Recurrence = request.Recurrence ?? string.Empty,
                    Place = request.Place
                };

                await _repository.AddAsync(lesson, cancellationToken);
            }

            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Schedule lessons created for group {GroupId}. Count: {Count}", request.GroupId, occurrences.Count);
            return Result.Ok($"Пари успішно додано. Створено: {occurrences.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating schedule lesson");
            return Result.Fail("Виникла помилка при створенні пари: " + ex.Message);
        }
    }

    public async Task<List<ScheduleLesson>> GetScheduleByGroupIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByGroupIdAsync(groupId, cancellationToken);
    }
}
