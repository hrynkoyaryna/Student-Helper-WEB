using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface ICacheableLookupService
{
    Task<List<Group>> GetAllGroupsAsync(CancellationToken cancellationToken = default);
    Task<List<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default);
    Task<List<Teacher>> GetAllTeachersAsync(CancellationToken cancellationToken = default);
}