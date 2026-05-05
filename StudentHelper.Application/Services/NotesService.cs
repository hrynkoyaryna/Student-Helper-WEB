using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Application.Models;

namespace StudentHelper.Application.Services;

public class NotesService : INotesService
{
    private readonly INotesRepository _repository;
    private readonly ILogger<NotesService> _logger;
    private readonly IOptions<ApplicationSettings> _settings;

    public NotesService(INotesRepository repository, ILogger<NotesService> logger, IOptions<ApplicationSettings> settings)
    {
        _repository = repository;
        _logger = logger;
        _settings = settings;
    }

    public async Task<List<Note>> GetUserNotesAsync(int userId, string? searchQuery = null)
    {
        var notes = await _repository.GetUserNotesAsync(userId);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var minSearchChars = _settings.Value.MinSearchCharacters;
            
            if (searchQuery.Length < minSearchChars)
            {
                _logger.LogWarning("Search query '{SearchQuery}' is too short. Minimum: {MinChars}", searchQuery, minSearchChars);
                return notes;
            }

            var lowerSearch = searchQuery.ToLower();
            notes = notes.Where(n =>
                n.Title.ToLower().Contains(lowerSearch) ||
                n.Body.ToLower().Contains(lowerSearch)).ToList();
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

    public async Task<Result> CreateNoteAsync(Note note)
    {
        await _repository.AddAsync(note);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Note {NoteId} created for user {UserId}", note.Id, note.UserId);
        return "Нотатка успішно створена";
    }

    public async Task<Result> UpdateNoteAsync(Note note, int userId)
    {
        var existingNote = await GetNoteByIdAsync(note.Id, userId);
        if (existingNote == null)
        {
            return Result.Fail("Нотатку не знайдено");
        }

        existingNote.Title = note.Title;
        existingNote.Body = note.Body;

        await _repository.UpdateAsync(existingNote);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Note {NoteId} updated for user {UserId}", note.Id, userId);

        return "Нотатка успішно оновлена";
    }

    public async Task<Result> DeleteNoteAsync(int id, int userId)
    {
        var note = await GetNoteByIdAsync(id, userId);
        if (note == null)
        {
            return Result.Fail("Нотатку не знайдено");
        }

        await _repository.DeleteAsync(note);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Note {NoteId} deleted for user {UserId}", id, userId);

        return "Нотатка успішно видалена";
    }

    public async Task<Result> PinNoteAsync(int id, int userId)
    {
        var note = await GetNoteByIdAsync(id, userId);
        if (note == null)
        {
            return Result.Fail("Нотатку не знайдено");
        }

        note.Pinned = true;
        await _repository.UpdateAsync(note);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Note {NoteId} pinned for user {UserId}", id, userId);

        return "Нотатка закріплена";
    }

    public async Task<Result> UnpinNoteAsync(int id, int userId)
    {
        var note = await GetNoteByIdAsync(id, userId);
        if (note == null)
        {
            return Result.Fail("Нотатку не знайдено");
        }

        note.Pinned = false;
        await _repository.UpdateAsync(note);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Note {NoteId} unpinned for user {UserId}", id, userId);

        return "Нотатка відкріплена";
    }
}
