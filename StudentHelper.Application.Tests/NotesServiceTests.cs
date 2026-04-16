using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Xunit;

namespace StudentHelper.Application.Tests;

public class NotesServiceTests
{
    private static NotesService CreateService(Mock<INotesRepository> repositoryMock)
    {
        var settings = Options.Create(new ApplicationSettings 
        { 
            MinSearchCharacters = 3,
            ItemsPerPage = 10,
            CalendarStartHour = 8,
            MaxTaskDescriptionLength = 500
        });
        return new NotesService(repositoryMock.Object, new NullLogger<NotesService>(), settings);
    }

    [Fact]
    public async Task CreateNoteAsync_Should_Call_Repository_Add_And_SaveChanges()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var service = CreateService(repositoryMock);

        var note = new Note
        {
            Title = "Моя перша нотатка",
            Body = "Це текст моєї перший нотатки",
            UserId = 1,
            Pinned = false
        };

        // Act
        var result = await service.CreateNoteAsync(note);

        // Assert
        Assert.True(result.Success);
        repositoryMock.Verify(r => r.AddAsync(note), Times.Once);
        repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUserNotesAsync_Should_Call_Repository_GetUserNotesAsync()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var notes = new List<Note>
        {
            new Note { Id = 1, Title = "Нотатка 1", Body = "Текст 1", UserId = 1, Pinned = true },
            new Note { Id = 2, Title = "Нотатка 2", Body = "Текст 2", UserId = 1, Pinned = false }
        };
        
        repositoryMock.Setup(r => r.GetUserNotesAsync(1))
            .ReturnsAsync(notes);

        var service = CreateService(repositoryMock);

        // Act
        var result = await service.GetUserNotesAsync(1);

        // Assert
        repositoryMock.Verify(r => r.GetUserNotesAsync(1), Times.Once);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUserNotesAsync_Should_Filter_By_Search_Query()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var notes = new List<Note>
        {
            new Note { Title = "Нотатка про CSharp", Body = "Текст про CSharp", UserId = 1 },
            new Note { Title = "Нотатка про JavaScript", Body = "Текст про JS", UserId = 1 }
        };
        
        repositoryMock.Setup(r => r.GetUserNotesAsync(1))
            .ReturnsAsync(notes);

        var service = CreateService(repositoryMock);

        // Act
        var result = await service.GetUserNotesAsync(1, "CSharp");

        // Assert
        Assert.Single(result);
        Assert.Contains("CSharp", result[0].Title);
    }

    [Fact]
    public async Task GetNoteByIdAsync_Should_Return_Note_For_Correct_User()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var note = new Note { Id = 1, Title = "Тестова нотатка", Body = "Текст", UserId = 1 };
        
        repositoryMock.Setup(r => r.GetNoteByIdAsync(1))
            .ReturnsAsync(note);

        var service = CreateService(repositoryMock);

        // Act
        var result = await service.GetNoteByIdAsync(1, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Тестова нотатка", result!.Title);
    }

    [Fact]
    public async Task GetNoteByIdAsync_Should_Return_Null_For_Wrong_User()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var note = new Note { Id = 1, Title = "Тестова нотатка", Body = "Текст", UserId = 1 };
        
        repositoryMock.Setup(r => r.GetNoteByIdAsync(1))
            .ReturnsAsync(note);

        var service = CreateService(repositoryMock);

        // Act
        var result = await service.GetNoteByIdAsync(1, 2);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateNoteAsync_Should_Update_Note()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var existingNote = new Note { Id = 1, Title = "Оригінальна нотатка", Body = "Оригінальний текст", UserId = 1 };
        
        repositoryMock.Setup(r => r.GetNoteByIdAsync(1))
            .ReturnsAsync(existingNote);

        var service = CreateService(repositoryMock);

        var updatedNote = new Note
        {
            Id = 1,
            Title = "Оновлена нотатка",
            Body = "Оновлений текст"
        };

        // Act
        var result = await service.UpdateNoteAsync(updatedNote, 1);

        // Assert
        Assert.True(result.Success);
        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Note>()), Times.Once);
    }

    [Fact]
    public async Task UpdateNoteAsync_Should_Return_False_For_Wrong_User()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var note = new Note { Id = 1, Title = "Нотатка", Body = "Текст", UserId = 1 };
        
        repositoryMock.Setup(r => r.GetNoteByIdAsync(1))
            .ReturnsAsync(note);

        var service = CreateService(repositoryMock);

        var updatedNote = new Note { Id = 1, Title = "Оновлена", Body = "Текст" };

        // Act
        var result = await service.UpdateNoteAsync(updatedNote, 2);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteNoteAsync_Should_Delete_Note()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var note = new Note { Id = 1, Title = "Нотатка для видалення", Body = "Текст", UserId = 1 };
        
        repositoryMock.Setup(r => r.GetNoteByIdAsync(1))
            .ReturnsAsync(note);

        var service = CreateService(repositoryMock);

        // Act
        var result = await service.DeleteNoteAsync(1, 1);

        // Assert
        Assert.True(result.Success);
        repositoryMock.Verify(r => r.DeleteAsync(note), Times.Once);
    }

    [Fact]
    public async Task DeleteNoteAsync_Should_Return_False_For_Wrong_User()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var note = new Note { Id = 1, Title = "Нотатка", Body = "Текст", UserId = 1 };
        
        repositoryMock.Setup(r => r.GetNoteByIdAsync(1))
            .ReturnsAsync(note);

        var service = CreateService(repositoryMock);

        // Act
        var result = await service.DeleteNoteAsync(1, 2);

        // Assert
        Assert.False(result.Success);
        repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Note>()), Times.Never);
    }

    [Fact]
    public async Task PinNoteAsync_Should_Pin_Note()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var note = new Note { Id = 1, Title = "Нотатка", Body = "Текст", UserId = 1, Pinned = false };
        
        repositoryMock.Setup(r => r.GetNoteByIdAsync(1))
            .ReturnsAsync(note);

        var service = CreateService(repositoryMock);

        // Act
        var result = await service.PinNoteAsync(1, 1);

        // Assert
        Assert.True(result.Success);
        Assert.True(note.Pinned);
        repositoryMock.Verify(r => r.UpdateAsync(note), Times.Once);
    }

    [Fact]
    public async Task UnpinNoteAsync_Should_Unpin_Note()
    {
        // Arrange
        var repositoryMock = new Mock<INotesRepository>();
        var note = new Note { Id = 1, Title = "Нотатка", Body = "Текст", UserId = 1, Pinned = true };
        
        repositoryMock.Setup(r => r.GetNoteByIdAsync(1))
            .ReturnsAsync(note);

        var service = CreateService(repositoryMock);

        // Act
        var result = await service.UnpinNoteAsync(1, 1);

        // Assert
        Assert.True(result.Success);
        Assert.False(note.Pinned);
        repositoryMock.Verify(r => r.UpdateAsync(note), Times.Once);
    }
}
