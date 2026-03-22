using StudentHelper.Application.Calendar.Models;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Calendar.Services;

public interface ICalendarService
{
    Task<CalendarOperationResult> CreateEventAsync(
        CreatePersonalEventRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PersonalEvent>> GetUserEventsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
