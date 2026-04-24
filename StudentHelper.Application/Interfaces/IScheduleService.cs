using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface IScheduleService
{
    Task<Result> CreateScheduleLessonAsync(CreateScheduleLessonRequest request, CancellationToken cancellationToken = default);

    Task<List<ScheduleLesson>> GetScheduleByGroupIdAsync(int groupId, CancellationToken cancellationToken = default);
}
