using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly StudentHelperDbContext _context;

    public TeacherRepository(StudentHelperDbContext context)
    {
        _context = context;
    }

    public async Task<List<Teacher>> GetAllAsync()
    {
        return await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
    }

    public async Task<Teacher?> GetByIdAsync(int id)
    {
        return await _context.Teachers.FindAsync(id);
    }

    public async Task<Teacher?> GetByNameAsync(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return null;
        var normalized = fullName.Trim();
        return await _context.Teachers.FirstOrDefaultAsync(t => t.FullName == normalized);
    }

    public Task AddAsync(Teacher teacher)
    {
        _context.Teachers.Add(teacher);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
