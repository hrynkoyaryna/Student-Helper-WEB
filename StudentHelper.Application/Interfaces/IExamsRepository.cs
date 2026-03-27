using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface IExamsRepository
{
    Task<List<Exam>> GetAllAsync();
    Task<Exam?> GetByIdAsync(int id);
    Task AddAsync(Exam exam);
    Task UpdateAsync(Exam exam);
    Task DeleteAsync(Exam exam);
    Task SaveChangesAsync();
}
