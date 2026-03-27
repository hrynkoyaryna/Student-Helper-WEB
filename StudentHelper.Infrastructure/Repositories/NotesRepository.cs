using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Repositories;

public class NotesRepository : INotesRepository
{
    private readonly StudentHelperDbContext _context;

    public NotesRepository(StudentHelperDbContext context)
    {
        _context = context;
    }

    public async Task<List<Note>> GetUserNotesAsync(int userId)
    {
        return await _context.Notes
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Pinned)
            .ThenByDescending(n => n.Id)
            .ToListAsync();
    }

    public async Task<Note?> GetNoteByIdAsync(int id)
    {
        return await _context.Notes.FirstOrDefaultAsync(n => n.Id == id);
    }

    public Task AddAsync(Note note)
    {
        _context.Notes.Add(note);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Note note)
    {
        _context.Notes.Update(note);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Note note)
    {
        _context.Notes.Remove(note);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
