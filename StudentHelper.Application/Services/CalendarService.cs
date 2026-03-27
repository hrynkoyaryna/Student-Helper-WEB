using Microsoft.Extensions.Logging;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Models.Calendar;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Services;

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

    public async Task<Result> CreateEventAsync(
        CreatePersonalEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0)
        {
            return Result.Fail("Некоректний користувач.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result.Fail("Назва події є обов'язковою.");
        }

        if (request.EndTime <= request.StartTime)
        {
            return Result.Fail("Час завершення має бути пізніше за час початку.");
        }

        var sameDayEvents = await this.personalEventRepository.GetByUserIdAndDateAsync(
            request.UserId,
            request.Date,
            cancellationToken);

        var localStart = request.Date.ToDateTime(request.StartTime);
        var localEnd = request.Date.ToDateTime(request.EndTime);

        var utcStart = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();
        var utcEnd = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime();

        var hasOverlap = sameDayEvents.Any(existing =>
            utcStart < existing.EndAt && utcEnd > existing.StartAt);

        if (hasOverlap)
        {
            this.logger.LogWarning(
                "Спроба створити подію з перетином. UserId: {UserId}, Title: {Title}",
                request.UserId,
                request.Title);

            return Result.Fail("Подія перетинається з уже існуючою подією.");
        }

        var personalEvent = new PersonalEvent
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            StartAt = utcStart,
            EndAt = utcEnd,
            UserId = request.UserId,
        };

        await this.personalEventRepository.AddAsync(personalEvent, cancellationToken);
        await this.personalEventRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation(
            "Створено подію. UserId: {UserId}, Title: {Title}",
            request.UserId,
            personalEvent.Title);

        return Result.Ok("Подія успішно створена");
    }

    public Task<IReadOnlyCollection<PersonalEvent>> GetUserEventsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return this.personalEventRepository.GetByUserIdAsync(userId, cancellationToken);
    }
}