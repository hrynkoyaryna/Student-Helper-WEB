using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface IScheduleRepository
{
    Task AddAsync(ScheduleLesson lesson, CancellationToken cancellationToken = default);

    Task<List<ScheduleLesson>> GetByGroupIdAsync(int groupId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
