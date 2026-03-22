using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface IPersonalEventRepository
{
    Task<IReadOnlyCollection<PersonalEvent>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PersonalEvent>> GetByUserIdAndDateAsync(
        int userId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PersonalEvent personalEvent,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}