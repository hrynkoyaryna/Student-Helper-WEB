using Microsoft.Extensions.Logging;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Services;

public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _repository;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(IScheduleRepository repository, ILogger<ScheduleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> CreateScheduleLessonAsync(CreateScheduleLessonRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndTime <= request.StartTime) return Result.Fail("Час завершення має бути пізніше за час початку.");
        if (request.GroupId <= 0) return Result.Fail("Виберіть групу.");

        if (request.SubjectId <= 0) return Result.Fail("Виберіть або введіть предмет.");

        var lesson = new ScheduleLesson
        {
            Date = request.Date.ToDateTime(request.StartTime),
            StartTime = request.StartTime.ToTimeSpan(),
            EndTime = request.EndTime.ToTimeSpan(),
            SubjectId = request.SubjectId,
            TeacherId = request.TeacherId ?? 0,
            GroupId = request.GroupId,
            Type = request.Type ?? string.Empty,
            Recurrence = request.Recurrence ?? string.Empty,
            Place = request.Place
        };

        await _repository.AddAsync(lesson, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return "Урок успішно додано";
    }

    public async Task<List<ScheduleLesson>> GetScheduleByGroupIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByGroupIdAsync(groupId, cancellationToken);
    }
}
