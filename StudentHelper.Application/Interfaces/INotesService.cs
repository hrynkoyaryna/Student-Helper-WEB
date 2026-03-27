using StudentHelper.Domain.Entities;
using StudentHelper.Application.Models;

namespace StudentHelper.Application.Interfaces;

public interface INotesService
{
    Task<List<Note>> GetUserNotesAsync(int userId, string? searchQuery = null);
    Task<Note?> GetNoteByIdAsync(int id, int userId);
    Task<Result> CreateNoteAsync(Note note);
    Task<Result> UpdateNoteAsync(Note note, int userId);
    Task<Result> DeleteNoteAsync(int id, int userId);
    Task<Result> PinNoteAsync(int id, int userId);
    Task<Result> UnpinNoteAsync(int id, int userId);
}
