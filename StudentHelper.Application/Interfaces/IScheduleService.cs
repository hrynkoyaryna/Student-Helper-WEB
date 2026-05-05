using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StudentHelper.Application.Interfaces
{
    public interface IScheduleService
    {
        Task<Result> CreateScheduleLessonAsync(CreateScheduleLessonRequest request, CancellationToken cancellationToken = default);
        Task<List<ScheduleLesson>> GetScheduleByGroupIdAsync(int groupId, CancellationToken cancellationToken = default);
        Task<Result> CreateLessonForGroupAsync(CreateScheduleLessonRequest request);
        Task<IEnumerable<ScheduleLesson>> GetGroupScheduleAsync(int groupId);
        Task<ScheduleLesson?> GetLessonByIdAsync(int lessonId);
        Task<Result> DeleteLessonAsync(int lessonId);
        Task<Result> CreateGroupAsync(string name);
        Task AddRangeAsync(IEnumerable<ScheduleLesson> lessons);
    }
}
