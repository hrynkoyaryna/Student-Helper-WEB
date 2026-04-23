#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Infrastructure.Repositories;
using Xunit;

namespace StudentHelper.Application.Tests;

/// <summary>
/// Integration tests for exam functionality.
/// These tests verify the interaction between multiple services and repositories
/// when testing complex scenarios like group exams appearing in calendars.
/// </summary>
public class ExamsIntegrationTests
{
    [Fact]
    public async Task CreateGroupExam_AndRetrieveForUser_WhenUserBelongsToGroup()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new StudentHelperDbContext(options);

        // Create a group
        var group = new Group { Id = 1, Name = "Group 2A" };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        // Create a user in the group
        var user = new User
        {
            Id = 1,
            UserName = "student1",
            Email = "student1@test.com",
            FirstName = "Іван",
            LastName = "Петренко",
            GroupId = 1  // User belongs to Group 2A
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Create an admin user
        var admin = new User
        {
            Id = 2,
            UserName = "admin1",
            Email = "admin1@test.com",
            FirstName = "Адмін",
            LastName = "Адміністратор",
            GroupId = null
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        // Create a teacher for the exam
        var teacher = new Teacher { Id = 1, FullName = "Іванов І.І." };
        context.Teachers.Add(teacher);
        await context.SaveChangesAsync();

        // Initialize services
        var examsRepository = new ExamsRepository(context);
        var teacherRepository = new Moq.Mock<ITeacherRepository>();
        var examsLogger = new Mock<ILogger<ExamsService>>();
        var settingsMock = Options.Create(new ApplicationSettings
        {
            MinSearchCharacters = 3,
            ItemsPerPage = 10,
            CalendarStartHour = 8,
            MaxTaskDescriptionLength = 500,
            PasswordSettings = new PasswordSettings()
        });

        var examsService = new ExamsService(examsRepository, teacherRepository.Object, examsLogger.Object, settingsMock);

        // Act - Create a group exam via the service
        var createRequest = new CreateGroupExamRequest
        {
            Subject = "Математика",
            DateTime = DateTime.UtcNow.AddDays(5),
            TeacherId = 1,
            Description = "Груповий екзамен для 2A",
            GroupId = 1,
            AdminUserId = 2  // Created by admin
        };

        var createResult = await examsService.CreateGroupExamAsync(createRequest);

        // Assert creation succeeded
        Assert.True(createResult.Success);

        // Act - Retrieve exams for the group
        var groupExams = await examsService.GetByGroupIdAsync(1);

        // Assert - Verify the exam appears in the group's exams
        Assert.NotNull(groupExams);
        Assert.Single(groupExams);
        
        var exam = groupExams[0];
        Assert.Equal("Математика", exam.Subject);
        Assert.Equal(1, exam.GroupId);
        Assert.Equal(2, exam.UserId);  // Created by admin
        Assert.Equal("Груповий екзамен для 2A", exam.Description);

        // Act - Verify the exam should appear for the user who belongs to the group
        var userGroupExams = groupExams.Where(e => e.GroupId == user.GroupId).ToList();

        // Assert - The exam should be visible to the user
        Assert.Single(userGroupExams);
        Assert.Equal("Математика", userGroupExams[0].Subject);
    }

    [Fact]
    public async Task UpdateGroupExam_AsAdmin_SucceedsWhenAdminIsCreator()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new StudentHelperDbContext(options);

        // Create group and users
        var group = new Group { Id = 1, Name = "Group A" };
        context.Groups.Add(group);

        var admin = new User
        {
            Id = 1,
            UserName = "admin1",
            Email = "admin1@test.com",
            FirstName = "Admin",
            LastName = "User"
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        // Create teacher
        var teacher = new Teacher { Id = 1, FullName = "Петров П.П." };
        context.Teachers.Add(teacher);
        
        var newTeacher = new Teacher { Id = 2, FullName = "Сидоренко С.С." };
        context.Teachers.Add(newTeacher);
        await context.SaveChangesAsync();

        // Create initial group exam
        var initialExam = new Exam
        {
            Subject = "Original Subject",
            DateTime = DateTime.UtcNow.AddDays(3),
            TeacherId = 1,
            UserId = 1,  // Created by admin
            GroupId = 1,
            Description = "Original"
        };
        context.Exams.Add(initialExam);
        await context.SaveChangesAsync();

        var examId = initialExam.Id;

        // Initialize service
        var examsRepository = new ExamsRepository(context);
        var teacherRepository = new Mock<ITeacherRepository>();
        teacherRepository.Setup(t => t.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((Teacher?)null);
        teacherRepository.Setup(t => t.AddAsync(It.IsAny<Teacher>()))
            .Returns(Task.CompletedTask);
        teacherRepository.Setup(t => t.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var examsLogger = new Mock<ILogger<ExamsService>>();
        var settingsMock = Options.Create(new ApplicationSettings
        {
            MinSearchCharacters = 3,
            ItemsPerPage = 10,
            CalendarStartHour = 8,
            MaxTaskDescriptionLength = 500,
            PasswordSettings = new PasswordSettings()
        });

        var examsService = new ExamsService(examsRepository, teacherRepository.Object, examsLogger.Object, settingsMock);

        // Act
        var updateRequest = new UpdateGroupExamRequest
        {
            Id = examId,
            Subject = "Updated Subject",
            DateTime = DateTime.UtcNow.AddDays(7),
            TeacherId = 2,
            Description = "Updated",
            AdminUserId = 1  // Same admin
        };

        var updateResult = await examsService.UpdateGroupExamAsync(updateRequest);

        // Assert
        Assert.True(updateResult.Success);
        
        var updatedExam = await examsService.GetExamByIdAsync(examId);
        Assert.NotNull(updatedExam);
        Assert.Equal("Updated Subject", updatedExam.Subject);
        Assert.Equal("Updated", updatedExam.Description);
    }

    [Fact]
    public async Task DeleteGroupExam_FailsWhenDifferentAdminAttempts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new StudentHelperDbContext(options);

        // Create users (two different admins)
        var admin1 = new User
        {
            Id = 1,
            UserName = "admin1",
            Email = "admin1@test.com",
            FirstName = "Admin",
            LastName = "One"
        };
        context.Users.Add(admin1);

        var admin2 = new User
        {
            Id = 2,
            UserName = "admin2",
            Email = "admin2@test.com",
            FirstName = "Admin",
            LastName = "Two"
        };
        context.Users.Add(admin2);
        await context.SaveChangesAsync();

        // Create group and teacher
        var group = new Group { Id = 1, Name = "Group B" };
        context.Groups.Add(group);

        var teacher = new Teacher { Id = 1, FullName = "Teacher T." };
        context.Teachers.Add(teacher);
        await context.SaveChangesAsync();

        // Create group exam by admin1
        var exam = new Exam
        {
            Subject = "Test Subject",
            DateTime = DateTime.UtcNow.AddDays(2),
            TeacherId = 1,
            UserId = 1,  // Created by admin1
            GroupId = 1
        };
        context.Exams.Add(exam);
        await context.SaveChangesAsync();

        var examId = exam.Id;

        // Initialize service
        var examsRepository = new ExamsRepository(context);
        var teacherRepository = new Mock<ITeacherRepository>();
        var examsLogger = new Mock<ILogger<ExamsService>>();
        var settingsMock = Options.Create(new ApplicationSettings
        {
            MinSearchCharacters = 3,
            ItemsPerPage = 10,
            CalendarStartHour = 8,
            MaxTaskDescriptionLength = 500,
            PasswordSettings = new PasswordSettings()
        });

        var examsService = new ExamsService(examsRepository, teacherRepository.Object, examsLogger.Object, settingsMock);

        // Act - Try to delete with admin2
        var deleteResult = await examsService.DeleteExamAsync(examId, 2);  // Different admin

        // Assert
        Assert.False(deleteResult.Success);
        Assert.Equal("Ви можете видаляти лише свої екзамени", deleteResult.Message);

        // Verify the exam still exists
        var stillExists = await examsService.GetExamByIdAsync(examId);
        Assert.NotNull(stillExists);
    }
}
