using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Infrastructure.Repositories;
using Xunit;

namespace StudentHelper.Application.Tests;

public class NotesRepositoryTests
{
    private static StudentHelperDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StudentHelperDbContext(options);
    }

    private static NotesRepository CreateRepository(StudentHelperDbContext context)
    {
        return new NotesRepository(context);
    }

    [Fact]
    public async Task GetUserNotesAsync_Should_Return_Only_User_Notes_Ordered_By_Pinned()
    {
        // Arrange
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var notes = new[]
        {
            new Note { Title = "Не закріплена 1", Body = "Текст 1", UserId = 1, Pinned = false },
            new Note { Title = "Закріплена 1", Body = "Текст 2", UserId = 1, Pinned = true },
            new Note { Title = "Чужа нотатка", Body = "Текст 3", UserId = 2, Pinned = true }
        };
        context.Notes.AddRange(notes);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetUserNotesAsync(1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result[0].Pinned);  // Закріплена першою
        Assert.False(result[1].Pinned); // Не закріплена другою
        Assert.All(result, n => Assert.Equal(1, n.UserId));
    }

    [Fact]
    public async Task GetNoteByIdAsync_Should_Return_Note()
    {
        // Arrange
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var note = new Note { Title = "Тестова нотатка", Body = "Текст", UserId = 1 };
        context.Notes.Add(note);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetNoteByIdAsync(note.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Тестова нотатка", result!.Title);
    }

    [Fact]
    public async Task GetNoteByIdAsync_Should_Return_Null_For_NonExistent_Note()
    {
        // Arrange
        using var context = CreateContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetNoteByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_Should_Add_Note()
    {
        // Arrange
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var note = new Note { Title = "Нова нотатка", Body = "Текст", UserId = 1 };

        // Act
        await repository.AddAsync(note);
        await repository.SaveChangesAsync();

        // Assert
        var savedNote = await context.Notes.FirstOrDefaultAsync();
        Assert.NotNull(savedNote);
        Assert.Equal("Нова нотатка", savedNote!.Title);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Note()
    {
        // Arrange
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var note = new Note { Title = "Оригінальна", Body = "Текст", UserId = 1 };
        context.Notes.Add(note);
        await context.SaveChangesAsync();

        // Act
        note.Title = "Оновлена";
        await repository.UpdateAsync(note);
        await repository.SaveChangesAsync();

        // Assert
        var updatedNote = await context.Notes.FirstAsync();
        Assert.Equal("Оновлена", updatedNote.Title);
    }

    [Fact]
    public async Task DeleteAsync_Should_Delete_Note()
    {
        // Arrange
        using var context = CreateContext();
        var repository = CreateRepository(context);

        var note = new Note { Title = "Нотатка для видалення", Body = "Текст", UserId = 1 };
        context.Notes.Add(note);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(note);
        await repository.SaveChangesAsync();

        // Assert
        var remainingNotes = await context.Notes.ToListAsync();
        Assert.Empty(remainingNotes);
    }
}
