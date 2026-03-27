#nullable enable
using System.Threading.Tasks;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Xunit;
using Microsoft.Extensions.Logging;
using System;

namespace StudentHelper.Application.Tests;

public class ExamsServiceTests
{
    [Fact]
    public async Task CreateExamAsync_CreatesExam_WhenTeacherNameProvidedAndNotExists()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

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

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object);

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

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object);

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

        var existingExam = new Exam { Id = 5, Subject = "Old", DateTime = DateTime.UtcNow.AddDays(1), TeacherId = 1 };
        examRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existingExam);

        teacherRepoMock.Setup(t => t.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Teacher?)null);
        teacherRepoMock.Setup(t => t.AddAsync(It.IsAny<Teacher>()))
            .Callback<Teacher>(t => t.Id = 99)
            .Returns(Task.CompletedTask);
        teacherRepoMock.Setup(t => t.SaveChangesAsync()).Returns(Task.CompletedTask);

        examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>())).Returns(Task.CompletedTask);
        examRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object);

        var request = new UpdateExamRequest
        {
            Id = 5,
            Subject = "NewSubject",
            DateTime = DateTime.Now.AddDays(2),
            TeacherName = "Петренко П.П."
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
    public async Task DeleteExamAsync_Deletes_WhenExists()
    {
        // Arrange
        var examRepoMock = new Mock<IExamsRepository>();
        var teacherRepoMock = new Mock<ITeacherRepository>();
        var loggerMock = new Mock<ILogger<ExamsService>>();

        var existingExam = new Exam { Id = 7, Subject = "Chemistry" };
        examRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(existingExam);
        examRepoMock.Setup(r => r.DeleteAsync(existingExam)).Returns(Task.CompletedTask);
        examRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object);

        // Act
        var result = await service.DeleteExamAsync(7);

        // Assert
        Assert.True(result.Success);
        examRepoMock.Verify(r => r.DeleteAsync(existingExam), Times.Once);
        examRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
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

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object);

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

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object);

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

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object);

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

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object);

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

        var service = new ExamsService(examRepoMock.Object, teacherRepoMock.Object, loggerMock.Object);

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
}
