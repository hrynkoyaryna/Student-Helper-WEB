using StudentHelper.Application.Models.Calendar;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Services;

public interface ICalendarService
{
    Task<Result> CreateEventAsync(
        CreatePersonalEventRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PersonalEvent>> GetUserEventsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<PersonalEvent?> GetEventByIdAsync(int eventId, CancellationToken cancellationToken = default);

    Task<Result> DeleteEventAsync(int eventId, int userId, CancellationToken cancellationToken = default);

    Task<Result> UpdateEventAsync(EditPersonalEventRequest request, CancellationToken cancellationToken = default);

    Task<List<dynamic>> GetFullCalendarDataAsync(int userId, CancellationToken cancellationToken = default);
}