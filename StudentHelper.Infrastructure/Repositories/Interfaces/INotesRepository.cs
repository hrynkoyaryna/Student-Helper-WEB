using StudentHelper.Domain.Entities;

namespace StudentHelper.Infrastructure.Repositories.Interfaces;

public interface INotesRepository
{
    Task<List<Note>> GetUserNotesAsync(int userId);
    Task<Note?> GetNoteByIdAsync(int id);
    Task AddAsync(Note note);
    Task UpdateAsync(Note note);
    Task DeleteAsync(Note note);
    Task SaveChangesAsync();
}
