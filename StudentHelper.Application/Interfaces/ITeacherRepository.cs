using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface ITeacherRepository
{
    Task<List<Teacher>> GetAllAsync();
    Task<Teacher?> GetByIdAsync(int id);
    Task<Teacher?> GetByNameAsync(string fullName);
    Task AddAsync(Teacher teacher);
    Task SaveChangesAsync();
}
