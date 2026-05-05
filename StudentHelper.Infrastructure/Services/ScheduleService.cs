using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Infrastructure.Services;

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
        try
        {
            var lesson = new ScheduleLesson
            {
                GroupId = request.GroupId,
                SubjectId = request.SubjectId,
                TeacherId = request.TeacherId ?? 0,
                Date = request.Date.ToDateTime(request.StartTime),
                DayOfWeek = request.Date.DayOfWeek,
                StartTime = request.StartTime.ToTimeSpan(),
                EndTime = request.EndTime.ToTimeSpan(),
                Room = request.Place ?? request.Room ?? "Н/Д",
                LessonType = request.Type ?? request.LessonType ?? "Лекція"
            };
            await _repository.AddAsync(lesson);
            await _repository.SaveChangesAsync();
            return Result.Ok("Урок успішно створено.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lesson");
            return Result.Fail("Помилка: " + ex.Message);
        }
    }

    public async Task<List<ScheduleLesson>> GetScheduleByGroupIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return new List<ScheduleLesson>(await _repository.GetByGroupIdAsync(groupId));
    }

    public async Task<Result> CreateLessonForGroupAsync(CreateScheduleLessonRequest request)
    {
        try
        {
            var lessonsToCreate = new List<ScheduleLesson>();
            DateTime current = request.StartDate;
            while (current.DayOfWeek != request.DayOfWeek) current = current.AddDays(1);

            while (current <= request.EndDate)
            {
                bool shouldAdd = true;
                if (request.IsEvenWeek.HasValue)
                {
                    int weekNumber = ISOWeek.GetWeekOfYear(current);
                    bool isEven = weekNumber % 2 == 0;
                    if (request.IsEvenWeek.Value != isEven) shouldAdd = false;
                }
                if (shouldAdd)
                {
                    lessonsToCreate.Add(new ScheduleLesson
                    {
                        GroupId = request.GroupId,
                        SubjectId = request.SubjectId,
                        TeacherId = request.TeacherId ?? 0,
                        DayOfWeek = request.DayOfWeek,
                        Date = current,
                        StartTime = request.StartTime.ToTimeSpan(),
                        EndTime = request.EndTime.ToTimeSpan(),
                        Room = request.Room ?? "Н/Д",
                        LessonType = request.LessonType ?? "Лекція",
                        IsEvenWeek = request.IsEvenWeek
                    });
                }
                current = current.AddDays(7);
            }

            if (!lessonsToCreate.Any()) return Result.Fail("Не знайдено жодної дати.");
            await _repository.AddRangeAsync(lessonsToCreate);
            await _repository.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Помилка: {ex.Message}");
        }
    }

    public async Task<IEnumerable<ScheduleLesson>> GetGroupScheduleAsync(int groupId)
    {
        return await _repository.GetByGroupIdAsync(groupId);
    }

    public async Task<ScheduleLesson?> GetLessonByIdAsync(int lessonId)
    {
        return await _repository.GetByIdAsync(lessonId);
    }

    public async Task<Result> DeleteLessonAsync(int lessonId)
    {
        var lesson = await _repository.GetByIdAsync(lessonId);
        if (lesson == null) return Result.Fail("Заняття не знайдено.");
        await _repository.DeleteAsync(lesson);
        return Result.Ok();
    }

    public async Task<Result> CreateGroupAsync(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name)) return Result.Fail("Назва не може бути порожньою.");
            var allGroups = await _repository.GetAllGroupsAsync();
            if (allGroups.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return Result.Fail("Група вже існує.");
            await _repository.CreateGroupAsync(new Group { Name = name });
            return Result.Ok();
        }
        catch (Exception ex) { return Result.Fail(ex.Message); }
    }

    public async Task AddRangeAsync(IEnumerable<ScheduleLesson> lessons)
    {
        await _repository.AddRangeAsync(lessons);
    }
}
