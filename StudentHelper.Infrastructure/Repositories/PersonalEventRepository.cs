using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Repositories;

public class PersonalEventRepository : IPersonalEventRepository
{
    private readonly StudentHelperDbContext dbContext;

    public PersonalEventRepository(StudentHelperDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<PersonalEvent>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await this.dbContext.PersonalEvents
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.StartAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PersonalEvent>> GetByUserIdAndDateAsync(
        int userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var localDateStart = date.ToDateTime(TimeOnly.MinValue);
        var localDateEnd = date.ToDateTime(new TimeOnly(23, 59, 59));

        var utcDateStart = DateTime.SpecifyKind(localDateStart, DateTimeKind.Local).ToUniversalTime();
        var utcDateEnd = DateTime.SpecifyKind(localDateEnd, DateTimeKind.Local).ToUniversalTime();

        return await this.dbContext.PersonalEvents
            .Where(x => x.UserId == userId && x.StartAt >= utcDateStart && x.StartAt <= utcDateEnd)
            .OrderBy(x => x.StartAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        PersonalEvent personalEvent,
        CancellationToken cancellationToken = default)
    {
        await this.dbContext.PersonalEvents.AddAsync(personalEvent, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return this.dbContext.SaveChangesAsync(cancellationToken);
    }
}