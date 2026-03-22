using Microsoft.Extensions.Logging;
using StudentHelper.Application.Abstractions.Repositories;
using StudentHelper.Application.Calendar.Models;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Calendar.Services;

public class CalendarService : ICalendarService
{
    private readonly IPersonalEventRepository personalEventRepository;
    private readonly ILogger<CalendarService> logger;

    public CalendarService(
        IPersonalEventRepository personalEventRepository,
        ILogger<CalendarService> logger)
    {
        this.personalEventRepository = personalEventRepository;
        this.logger = logger;
    }

    public async Task<CalendarOperationResult> CreateEventAsync(
        CreatePersonalEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0)
        {
            return CalendarOperationResult.Fail("Некоректний користувач.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return CalendarOperationResult.Fail("Назва події є обов’язковою.");
        }

        if (request.EndTime <= request.StartTime)
        {
            return CalendarOperationResult.Fail("Час завершення має бути пізніше за час початку.");
        }

        var sameDayEvents = await this.personalEventRepository.GetByUserIdAndDateAsync(
            request.UserId,
            request.Date,
            cancellationToken);

        var newStart = request.Date.ToDateTime(request.StartTime);
        var newEnd = request.Date.ToDateTime(request.EndTime);

        var hasOverlap = sameDayEvents.Any(existing =>
            newStart < existing.EndAt && newEnd > existing.StartAt);

        if (hasOverlap)
        {
            this.logger.LogWarning(
                "Спроба створити подію з перетином. UserId: {UserId}, Title: {Title}",
                request.UserId,
                request.Title);

            return CalendarOperationResult.Fail("Подія перетинається з уже існуючою подією.");
        }

        var personalEvent = new PersonalEvent
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            StartAt = newStart,
            EndAt = newEnd,
            UserId = request.UserId,
        };

        await this.personalEventRepository.AddAsync(personalEvent, cancellationToken);
        await this.personalEventRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation(
            "Створено подію. UserId: {UserId}, Title: {Title}",
            request.UserId,
            personalEvent.Title);

        return CalendarOperationResult.Ok();
    }

    public Task<IReadOnlyCollection<PersonalEvent>> GetUserEventsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return this.personalEventRepository.GetByUserIdAsync(userId, cancellationToken);
    }
}
