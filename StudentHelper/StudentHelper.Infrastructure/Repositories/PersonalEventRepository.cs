using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Abstractions.Repositories;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Repositories;

public class PersonalEventRepository : IPersonalEventRepository
{
    private readonly StudentHelperDbContext _dbContext;

    public PersonalEventRepository(StudentHelperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<PersonalEvent>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PersonalEvents
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.StartAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PersonalEvent>> GetByUserIdAndDateAsync(
        int userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var dateStart = date.ToDateTime(TimeOnly.MinValue);
        var dateEnd = date.ToDateTime(TimeOnly.MaxValue);

        return await _dbContext.PersonalEvents
            .Where(x => x.UserId == userId && x.StartAt >= dateStart && x.StartAt <= dateEnd)
            .OrderBy(x => x.StartAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        PersonalEvent personalEvent,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PersonalEvents.AddAsync(personalEvent, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
