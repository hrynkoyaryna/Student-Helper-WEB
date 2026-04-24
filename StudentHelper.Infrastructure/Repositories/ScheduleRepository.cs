using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private readonly StudentHelperDbContext _context;

    public ScheduleRepository(StudentHelperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ScheduleLesson lesson, CancellationToken cancellationToken = default)
    {
        await _context.ScheduleLessons.AddAsync(lesson, cancellationToken);
    }

    public async Task<List<ScheduleLesson>> GetByGroupIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduleLessons
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
