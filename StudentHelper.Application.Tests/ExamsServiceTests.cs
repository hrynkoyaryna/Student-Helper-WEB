#nullable enable
using System.Threading.Tasks;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace StudentHelper.Application.Tests;

public class ExamsServiceTests
{
    private static IOptions<ApplicationSettings> CreateMockSettings()
    {
        return Options.Create(new ApplicationSettings 
        { 
            MinSearchCharacters = 3,
            ItemsPerPage = 10,
            CalendarStartHour = 8,
            MaxTaskDescriptionLength = 500,
            PasswordSettings = new PasswordSettings()
        });
    }

    [Fact]
    public async Task CreateExamAsync_CreatesExam_WhenTeacherNameProvidedAndNotExists()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();
        var settingsMock = CreateMockSettings();

        // Teacher does not exist
        teacherRepoMock.Setup(t => t.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((Teacher?)null);

        Teacher addedTeacher = null!;
        teacherRepoMock.Setup(t => t.AddAsync(It.IsAny<Teacher>()))
            .Callback<Teacher>(t => {
                // simulate assignment of Id as would happen in DB
                t.Id = 42;
                addedTeacher = t;
            })
            .Returns(Task.CompletedTask);

        teacherRepoMock.Setup(t => t.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        Exam addedExam = null!;
        examRepoMock.Setup(r => r.AddAsync(It.IsAny<Exam>()))
            .Callback<Exam>(e => addedExam = e)
            .Returns(Task.CompletedTask);

        examRepoMock.Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, settingsMock);

        var request = new CreateExamRequest
        {
            Subject = "Math",
            DateTime = DateTime.Now.AddDays(3),
            TeacherName = "Іванов І.І."
        };

        // Act
        var result = await service.CreateExamAsync(request);

        // Assert
        Assert.True(result.Success);
        teacherRepoMock.Verify(t => t.GetByNameAsync("Іванов І.І."), Times.Once);
        teacherRepoMock.Verify(t => t.AddAsync(It.IsAny<Teacher>()), Times.Once);
        teacherRepoMock.Verify(t => t.SaveChangesAsync(), Times.Once);
        examRepoMock.Verify(r => r.AddAsync(It.IsAny<Exam>()), Times.Once);
        examRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        Assert.NotNull(addedTeacher);
        Assert.Equal(42, addedExam.TeacherId);
        Assert.Equal(DateTimeKind.Utc, addedExam.DateTime.Kind);
    }

    [Fact]
    public async Task CreateExamAsync_ReturnsFail_WhenNoTeacherProvided()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new CreateExamRequest
        {
            Subject = "Physics",
            DateTime = DateTime.Now.AddDays(1),
            TeacherId = null,
            TeacherName = null
        };

        // Act
        var result = await service.CreateExamAsync(request);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateExamAsync_UpdatesExamAndCreatesTeacherWhenNeeded()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var existingExam = new Exam { Id = 5, Subject = "Old", DateTime = DateTime.UtcNow.AddDays(1), TeacherId = 1, UserId = 1 };
        examRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existingExam);

        teacherRepoMock.Setup(t => t.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Teacher?)null);
        teacherRepoMock.Setup(t => t.AddAsync(It.IsAny<Teacher>()))
            .Callback<Teacher>(t => t.Id = 99)
            .Returns(Task.CompletedTask);
        teacherRepoMock.Setup(t => t.SaveChangesAsync()).Returns(Task.CompletedTask);

        examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>())).Returns(Task.CompletedTask);
        examRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new UpdateExamRequest
        {
            Id = 5,
            Subject = "NewSubject",
            DateTime = DateTime.Now.AddDays(2),
            TeacherName = "Петренко П.П.",
            UserId = 1
        };

        // Act
        var result = await service.UpdateExamAsync(request);

        // Assert
        Assert.True(result.Success);
        teacherRepoMock.Verify(t => t.AddAsync(It.IsAny<Teacher>()), Times.Once);
        examRepoMock.Verify(r => r.UpdateAsync(It.Is<Exam>(e => e.Id == 5 && e.Subject == "NewSubject")), Times.Once);
        examRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateExamAsync_ReturnsFail_WhenUserNotOwner()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var existingExam = new Exam { Id = 5, Subject = "Old", DateTime = DateTime.UtcNow.AddDays(1), TeacherId = 1, UserId = 2 };
        examRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existingExam);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new UpdateExamRequest
        {
            Id = 5,
            Subject = "NewSubject",
            DateTime = DateTime.Now.AddDays(2),
            TeacherName = "Петренко П.П.",
            UserId = 1
        };

        // Act
        var result = await service.UpdateExamAsync(request);

        // Assert
        Assert.False(result.Success);
        examRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Exam>()), Times.Never);
    }

    [Fact]
    public async Task DeleteExamAsync_Deletes_WhenExists()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var existingExam = new Exam { Id = 7, Subject = "Chemistry", UserId = 1 };
        examRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(existingExam);
        examRepoMock.Setup(r => r.DeleteAsync(existingExam)).Returns(Task.CompletedTask);
        examRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        // Act
        var result = await service.DeleteExamAsync(7, 1);

        // Assert
        Assert.True(result.Success);
        examRepoMock.Verify(r => r.DeleteAsync(existingExam), Times.Once);
        examRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteExamAsync_ReturnsFail_WhenExamNotFound()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        examRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Exam?)null);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        // Act
        var result = await service.DeleteExamAsync(999, 1);

        // Assert
        Assert.False(result.Success);
        examRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Exam>()), Times.Never);
    }

    [Fact]
    public async Task DeleteExamAsync_ReturnsFail_WhenUserNotOwner()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var existingExam = new Exam { Id = 7, Subject = "Chemistry", UserId = 2 };
        examRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(existingExam);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        // Act
        var result = await service.DeleteExamAsync(7, 1);

        // Assert
        Assert.False(result.Success);
        examRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Exam>()), Times.Never);
    }

    [Fact]
    public async Task CreateExamAsync_UsesExistingTeacher_WhenTeacherExists()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var existingTeacher = new Teacher { Id = 10, FullName = "Сидоренко С.С." };
        teacherRepoMock.Setup(t => t.GetByNameAsync("Сидоренко С.С.")).ReturnsAsync(existingTeacher);

        Exam addedExam = null!;
        examRepoMock.Setup(r => r.AddAsync(It.IsAny<Exam>()))
            .Callback<Exam>(e => addedExam = e)
            .Returns(Task.CompletedTask);
        examRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new CreateExamRequest
        {
            Subject = "History",
            DateTime = DateTime.Now.AddDays(5),
            TeacherName = "Сидоренко С.С."
        };

        // Act
        var result = await service.CreateExamAsync(request);

        // Assert
        Assert.True(result.Success);
        teacherRepoMock.Verify(t => t.GetByNameAsync("Сидоренко С.С."), Times.Once);
        teacherRepoMock.Verify(t => t.AddAsync(It.IsAny<Teacher>()), Times.Never);
        examRepoMock.Verify(r => r.AddAsync(It.IsAny<Exam>()), Times.Once);
        Assert.Equal(10, addedExam.TeacherId);
        Assert.Equal(DateTimeKind.Utc, addedExam.DateTime.Kind);
    }

    [Fact]
    public async Task CreateExamAsync_ReturnsFail_WhenSubjectEmpty()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new CreateExamRequest
        {
            Subject = "   ",
            DateTime = DateTime.Now.AddDays(2),
            TeacherName = "Any"
        };

        // Act
        var result = await service.CreateExamAsync(request);

        // Assert
        Assert.False(result.Success);
        examRepoMock.Verify(r => r.AddAsync(It.IsAny<Exam>()), Times.Never);
    }

    [Fact]
    public async Task CreateExamAsync_ReturnsFail_WhenDateIsDefault()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new CreateExamRequest
        {
            Subject = "Biology",
            DateTime = default,
            TeacherName = "Any"
        };

        // Act
        var result = await service.CreateExamAsync(request);

        // Assert
        Assert.False(result.Success);
        examRepoMock.Verify(r => r.AddAsync(It.IsAny<Exam>()), Times.Never);
    }

    [Fact]
    public async Task UpdateExamAsync_ReturnsFail_WhenExamNotFound()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        examRepoMock.Setup(r => r.GetByIdAsync(123)).ReturnsAsync((Exam?)null);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new UpdateExamRequest
        {
            Id = 123,
            Subject = "Test",
            DateTime = DateTime.Now.AddDays(1),
            TeacherName = "T"
        };

        // Act
        var result = await service.UpdateExamAsync(request);

        // Assert
        Assert.False(result.Success);
        examRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Exam>()), Times.Never);
    }

    [Fact]
    public async Task CreateExamAsync_PropagatesException_WhenRepositoryThrows()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        teacherRepoMock.Setup(t => t.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Teacher?)null);
        teacherRepoMock.Setup(t => t.AddAsync(It.IsAny<Teacher>())).Returns(Task.CompletedTask);
        teacherRepoMock.Setup(t => t.SaveChangesAsync()).Returns(Task.CompletedTask);

        examRepoMock.Setup(r => r.AddAsync(It.IsAny<Exam>())).ThrowsAsync(new InvalidOperationException("DB error"));

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new CreateExamRequest
        {
            Subject = "Geo",
            DateTime = DateTime.Now.AddDays(1),
            TeacherName = "New Teacher"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.CreateExamAsync(request));
        Assert.Equal("DB error", exception.Message);
    }

    // Group Exam Tests
    [Fact]
    public async Task CreateGroupExamAsync_CreatesGroupExam_WhenAdminProvides()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();
        var settingsMock = CreateMockSettings();

        // Teacher does not exist
        teacherRepoMock.Setup(t => t.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((Teacher?)null);

        Teacher addedTeacher = null!;
        teacherRepoMock.Setup(t => t.AddAsync(It.IsAny<Teacher>()))
            .Callback<Teacher>(t => {
                t.Id = 50;
                addedTeacher = t;
            })
            .Returns(Task.CompletedTask);

        teacherRepoMock.Setup(t => t.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        Exam addedExam = null!;
        examRepoMock.Setup(r => r.AddAsync(It.IsAny<Exam>()))
            .Callback<Exam>(e => addedExam = e)
            .Returns(Task.CompletedTask);

        examRepoMock.Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, settingsMock);

        var request = new CreateGroupExamRequest
        {
            Subject = "Mathematics",
            DateTime = DateTime.Now.AddDays(4),
            TeacherName = "Петров П.П.",
            GroupId = 5,
            AdminUserId = 1
        };

        // Act
        var result = await service.CreateGroupExamAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(addedExam);
        Assert.Equal(5, addedExam.GroupId);
        Assert.Equal(1, addedExam.UserId);
        Assert.Equal(50, addedExam.TeacherId);
        Assert.Equal("Mathematics", addedExam.Subject);
        Assert.Equal(DateTimeKind.Local, addedExam.DateTime.Kind);
        examRepoMock.Verify(r => r.AddAsync(It.IsAny<Exam>()), Times.Once);
        examRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateGroupExamAsync_ReturnsFail_WhenGroupIdInvalid()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new CreateGroupExamRequest
        {
            Subject = "Physics",
            DateTime = DateTime.Now.AddDays(3),
            TeacherName = "Teacher",
            GroupId = 0,  // Invalid group ID
            AdminUserId = 1
        };

        // Act
        var result = await service.CreateGroupExamAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Виберіть групу", result.Message);
        examRepoMock.Verify(r => r.AddAsync(It.IsAny<Exam>()), Times.Never);
    }

    [Fact]
    public async Task UpdateGroupExamAsync_UpdatesGroupExam_WhenAdminIsCreator()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var existingGroupExam = new Exam 
        { 
            Id = 10, 
            Subject = "Old Subject", 
            DateTime = DateTime.UtcNow.AddDays(1), 
            TeacherId = 1,
            UserId = 5,  // Admin who created it
            GroupId = 3  // Group exam
        };

        examRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(existingGroupExam);

        teacherRepoMock.Setup(t => t.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((Teacher?)null);

        teacherRepoMock.Setup(t => t.AddAsync(It.IsAny<Teacher>()))
            .Callback<Teacher>(t => t.Id = 99)
            .Returns(Task.CompletedTask);

        teacherRepoMock.Setup(t => t.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>()))
            .Returns(Task.CompletedTask);

        examRepoMock.Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new UpdateGroupExamRequest
        {
            Id = 10,
            Subject = "Updated Subject",
            DateTime = DateTime.Now.AddDays(5),
            TeacherName = "New Teacher",
            AdminUserId = 5  // Same admin who created it
        };

        // Act
        var result = await service.UpdateGroupExamAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Updated Subject", existingGroupExam.Subject);
        examRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Exam>()), Times.Once);
        examRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateGroupExamAsync_ReturnsFail_WhenAdminIsNotCreator()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var existingGroupExam = new Exam 
        { 
            Id = 10, 
            Subject = "Original", 
            DateTime = DateTime.UtcNow.AddDays(1), 
            TeacherId = 1,
            UserId = 5,  // Admin who created it
            GroupId = 3
        };

        examRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(existingGroupExam);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        var request = new UpdateGroupExamRequest
        {
            Id = 10,
            Subject = "New Subject",
            DateTime = DateTime.Now.AddDays(5),
            TeacherName = "Another Teacher",
            AdminUserId = 6  // Different admin
        };

        // Act
        var result = await service.UpdateGroupExamAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Тільки адміністратор, який створив цей екзамен, може його редагувати", result.Message);
        examRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Exam>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGroupExamAsync_CanDelete_WhenAdminIsCreator()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var existingGroupExam = new Exam 
        { 
            Id = 15, 
            Subject = "Group Exam", 
            TeacherId = 1,
            UserId = 5,  // Admin who created it
            GroupId = 4
        };

        examRepoMock.Setup(r => r.GetByIdAsync(15)).ReturnsAsync(existingGroupExam);
        examRepoMock.Setup(r => r.DeleteAsync(existingGroupExam)).Returns(Task.CompletedTask);
        examRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        // Act
        var result = await service.DeleteExamAsync(15, 5);  // Same admin who created it

        // Assert
        Assert.True(result.Success);
        examRepoMock.Verify(r => r.DeleteAsync(existingGroupExam), Times.Once);
        examRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupExamAsync_ReturnsFail_WhenAdminIsNotCreator()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var existingGroupExam = new Exam 
        { 
            Id = 15, 
            Subject = "Group Exam", 
            UserId = 5,  // Admin who created it
            GroupId = 4
        };

        examRepoMock.Setup(r => r.GetByIdAsync(15)).ReturnsAsync(existingGroupExam);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object, CreateMockSettings());

        // Act
        var result = await service.DeleteExamAsync(15, 6);  // Different admin

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Ви можете видаляти лише свої екзамени", result.Message);
        examRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Exam>()), Times.Never);
    }
}
