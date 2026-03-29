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
        if (request.UserId <= 0) return Result.Fail("Некоректний користувач.");
        if (string.IsNullOrWhiteSpace(request.Title)) return Result.Fail("Назва події є обов'язковою.");
        if (request.EndTime <= request.StartTime) return Result.Fail("Час завершення має бути пізніше за час початку.");

        var start = request.Date.ToDateTime(request.StartTime);
        var end = request.Date.ToDateTime(request.EndTime);

        var sameDayEvents = await this.personalEventRepository.GetByUserIdAndDateAsync(
            request.UserId,
            request.Date,
            cancellationToken);

        var hasOverlap = sameDayEvents.Any(existing =>
            start < existing.EndAt && end > existing.StartAt);

        string resultMessage = "Подію успішно створено!";

        if (hasOverlap)
        {
            resultMessage = "Подію успішно створено, але вона ПЕРЕТИНАЄТЬСЯ у часі з іншою вашою подією!";
            this.logger.LogInformation("Створено подію з перетином. UserId: {UserId}", request.UserId);
        }

        var personalEvent = new PersonalEvent
        {
            Title = request.Title.Trim(),
            Description = (request.Description ?? string.Empty).Trim(),
            StartAt = start,
            EndAt = end,
            UserId = request.UserId,
            Color = request.Color 
        };

        await this.personalEventRepository.AddAsync(personalEvent, cancellationToken);
        await this.personalEventRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok(resultMessage);
    }

    public Task<IReadOnlyCollection<PersonalEvent>> GetUserEventsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return this.personalEventRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<PersonalEvent?> GetEventByIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await this.personalEventRepository.GetByIdAsync(eventId, cancellationToken);
    }

    public async Task<Result> DeleteEventAsync(int eventId, int userId, CancellationToken cancellationToken = default)
    {
        var personalEvent = await this.personalEventRepository.GetByIdAsync(eventId, cancellationToken);
        
        if (personalEvent == null) return Result.Fail("Подію не знайдено.");
        if (personalEvent.UserId != userId) return Result.Fail("У вас немає прав для видалення цієї події.");

        await this.personalEventRepository.DeleteAsync(personalEvent, cancellationToken);
        await this.personalEventRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok("Подію успішно видалено.");
    }
    
    public async Task<Result> UpdateEventAsync(EditPersonalEventRequest request, CancellationToken cancellationToken = default)
    {
        var personalEvent = await this.personalEventRepository.GetByIdAsync(request.Id, cancellationToken);

        if (personalEvent == null) return Result.Fail("Подію не знайдено.");
        if (personalEvent.UserId != request.UserId) return Result.Fail("У вас немає прав для редагування цієї події.");
        if (string.IsNullOrWhiteSpace(request.Title)) return Result.Fail("Назва події є обов'язковою.");
        if (request.EndTime <= request.StartTime) return Result.Fail("Час завершення має бути пізніше за час початку.");

        var start = request.Date.ToDateTime(request.StartTime);
        var end = request.Date.ToDateTime(request.EndTime);

        var sameDayEvents = await this.personalEventRepository.GetByUserIdAndDateAsync(
            request.UserId,
            request.Date,
            cancellationToken);

        var hasOverlap = sameDayEvents.Any(existing =>
            existing.Id != request.Id &&
            start < existing.EndAt && end > existing.StartAt);

        string resultMessage = "Подію успішно оновлено!";

        if (hasOverlap)
        {
            resultMessage = "Подію успішно оновлено, але вона ПЕРЕТИНАЄТЬСЯ у часі з іншою вашою подією!";
            this.logger.LogInformation("Оновлено подію з перетином. UserId: {UserId}, EventId: {EventId}", request.UserId, request.Id);
        }

        personalEvent.Title = request.Title.Trim();
        personalEvent.Description = (request.Description ?? string.Empty).Trim();
        personalEvent.StartAt = start;
        personalEvent.EndAt = end;
        personalEvent.Color = request.Color; 

        await this.personalEventRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok(resultMessage);
    }
}