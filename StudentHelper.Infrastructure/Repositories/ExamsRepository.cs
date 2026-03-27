using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Repositories;

public class ExamsRepository : IExamsRepository
{
    private readonly StudentHelperDbContext _context;

    public ExamsRepository(StudentHelperDbContext context)
    {
        _context = context;
    }

    public async Task<List<Exam>> GetAllAsync()
    {
        return await _context.Exams
            .Include(e => e.Teacher)
            .OrderBy(e => e.DateTime)
            .ToListAsync();
    }

    public async Task<Exam?> GetByIdAsync(int id)
    {
        return await _context.Exams
            .Include(e => e.Teacher)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task AddAsync(Exam exam)
    {
        _context.Exams.Add(exam);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Exam exam)
    {
        _context.Exams.Update(exam);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Exam exam)
    {
        _context.Exams.Remove(exam);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
