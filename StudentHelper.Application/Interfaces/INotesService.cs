using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface INotesService
{
    Task<List<Note>> GetUserNotesAsync(int userId, string? searchQuery = null);
    Task<Note?> GetNoteByIdAsync(int id, int userId);
    Task CreateNoteAsync(Note note);
    Task<bool> UpdateNoteAsync(Note note, int userId);
    Task<bool> DeleteNoteAsync(int id, int userId);
    Task<bool> PinNoteAsync(int id, int userId);
    Task<bool> UnpinNoteAsync(int id, int userId);
}
