using Microsoft.Extensions.Logging;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Repositories.Interfaces;

namespace StudentHelper.Infrastructure.Services;

public class NotesService : INotesService
{
    private readonly INotesRepository _repository;
    private readonly ILogger<NotesService> _logger;

    public NotesService(INotesRepository repository, ILogger<NotesService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<Note>> GetUserNotesAsync(int userId, string? searchQuery = null)
    {
        var notes = await _repository.GetUserNotesAsync(userId);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var lowerSearch = searchQuery.ToLower();
            notes = notes.Where(n => 
                n.Title.ToLower().Contains(lowerSearch) || 
                n.Body.ToLower().Contains(lowerSearch)
            ).ToList();
        }

        return notes;
    }

    public async Task<Note?> GetNoteByIdAsync(int id, int userId)
    {
        var note = await _repository.GetNoteByIdAsync(id);
        
        if (note == null || note.UserId != userId)
        {
            return null;
        }

        return note;
    }

    public async Task CreateNoteAsync(Note note)
    {
        await _repository.AddAsync(note);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Note {NoteId} created for user {UserId}", note.Id, note.UserId);
    }

    public async Task<bool> UpdateNoteAsync(Note note, int userId)
    {
        var existingNote = await GetNoteByIdAsync(note.Id, userId);
        if (existingNote == null)
        {
            return false;
        }

        existingNote.Title = note.Title;
        existingNote.Body = note.Body;

        await _repository.UpdateAsync(existingNote);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Note {NoteId} updated for user {UserId}", note.Id, userId);

        return true;
    }

    public async Task<bool> DeleteNoteAsync(int id, int userId)
    {
        var note = await GetNoteByIdAsync(id, userId);
        if (note == null)
        {
            return false;
        }

        await _repository.DeleteAsync(note);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Note {NoteId} deleted for user {UserId}", id, userId);

        return true;
    }

    public async Task<bool> PinNoteAsync(int id, int userId)
    {
        var note = await GetNoteByIdAsync(id, userId);
        if (note == null)
        {
            return false;
        }

        note.Pinned = true;
        await _repository.UpdateAsync(note);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Note {NoteId} pinned for user {UserId}", id, userId);

        return true;
    }

    public async Task<bool> UnpinNoteAsync(int id, int userId)
    {
        var note = await GetNoteByIdAsync(id, userId);
        if (note == null)
        {
            return false;
        }

        note.Pinned = false;
        await _repository.UpdateAsync(note);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Note {NoteId} unpinned for user {UserId}", id, userId);

        return true;
    }
}
