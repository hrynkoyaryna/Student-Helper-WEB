using StudentHelper.Application.Models.Calendar;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Services;

public interface ICalendarService
{
    Task<CalendarOperationResult> CreateEventAsync(
        CreatePersonalEventRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PersonalEvent>> GetUserEventsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}