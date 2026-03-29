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

    public async Task<PersonalEvent?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await this.dbContext.PersonalEvents.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PersonalEvent>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await this.dbContext.PersonalEvents.Where(e => e.UserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PersonalEvent>> GetByUserIdAndDateAsync(
        int userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue);

        return await this.dbContext.PersonalEvents
            .Where(e => e.UserId == userId && e.StartAt >= startOfDay && e.StartAt <= endOfDay)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PersonalEvent personalEvent, CancellationToken cancellationToken = default)
    {
        await this.dbContext.PersonalEvents.AddAsync(personalEvent, cancellationToken);
    }

    public Task DeleteAsync(PersonalEvent personalEvent, CancellationToken cancellationToken = default)
    {
        this.dbContext.PersonalEvents.Remove(personalEvent);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}