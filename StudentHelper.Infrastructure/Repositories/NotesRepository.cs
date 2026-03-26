using Microsoft.EntityFrameworkCore;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Infrastructure.Repositories.Interfaces;

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

    public async Task AddAsync(Note note)
    {
        _context.Notes.Add(note);
    }

    public async Task UpdateAsync(Note note)
    {
        _context.Notes.Update(note);
    }

    public async Task DeleteAsync(Note note)
    {
        _context.Notes.Remove(note);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
