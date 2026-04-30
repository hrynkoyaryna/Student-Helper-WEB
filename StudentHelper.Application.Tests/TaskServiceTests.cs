using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Infrastructure.Services;
using Xunit;

namespace StudentHelper.Application.Tests;

public class TaskServiceTests
{
    private static StudentHelperDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StudentHelperDbContext(options);
    }

    private static TaskService CreateService(StudentHelperDbContext context)
    {
        var settings = Options.Create(new ApplicationSettings
        {
            MinSearchCharacters = 3,
            ItemsPerPage = 10,
            CalendarStartHour = 8,
            MaxTaskDescriptionLength = 500
        });

        return new TaskService(context, new NullLogger<TaskService>(), settings);
    }

    [Fact]
    public async Task CreateTaskAsync_Should_Add_Task()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var task = new TaskItem
        {
            Title = "Тестове завдання",
            Deadline = DateTime.UtcNow.AddDays(1),
            Status = "Поточне",
            Subject = "Програмування",
            UserId = 1,
        };

        var result = await service.CreateTaskAsync(task);

        Assert.True(result.Success);

        var savedTask = await context.Tasks.FirstOrDefaultAsync();

        Assert.NotNull(savedTask);
        Assert.Equal("Тестове завдання", savedTask!.Title);
        Assert.Equal(1, savedTask.UserId);
    }

    [Fact]
    public async Task CreateTaskAsync_Should_Set_Overdue_When_Deadline_In_Past()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var task = new TaskItem
        {
            Title = "Стара задача",
            Deadline = DateTime.UtcNow.AddDays(-1),
            Status = "Поточне",
            Subject = "БД",
            UserId = 1,
        };

        var result = await service.CreateTaskAsync(task);

        Assert.True(result.Success);

        var savedTask = await context.Tasks.FirstAsync();
        Assert.Equal("Прострочене", savedTask.Status);
    }

    [Fact]
    public async Task GetUserTasksAsync_Should_Return_Only_User_Tasks()
    {
        using var context = CreateContext();

        context.Tasks.AddRange(
            new TaskItem
            {
                Title = "Моє завдання",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "Програмна інженерія",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Чуже завдання",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "Фізика",
                UserId = 2,
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetUserTasksAsync(1);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("Моє завдання", result.Value[0].Title);
    }

    [Fact]
    public async Task UpdateTaskAsync_Should_Update_Task_When_Owner_Matches()
    {
        using var context = CreateContext();

        var existingTask = new TaskItem
        {
            Title = "Старе завдання",
            Deadline = DateTime.UtcNow.AddDays(1),
            Status = "Поточне",
            Subject = "Математика",
            UserId = 1,
        };

        context.Tasks.Add(existingTask);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var updatedTask = new TaskItem
        {
            Id = existingTask.Id,
            Title = "Нове завдання",
            Deadline = DateTime.UtcNow.AddDays(2),
            Status = "Виконане",
            Subject = "Математика",
        };

        var result = await service.UpdateTaskAsync(updatedTask, 1);

        Assert.True(result.Success);

        var savedTask = await context.Tasks.FirstAsync();
        Assert.Equal("Нове завдання", savedTask.Title);
        Assert.Equal("Виконане", savedTask.Status);
    }

    [Fact]
    public async Task UpdateTaskAsync_Should_Return_False_When_Task_Belongs_To_Another_User()
    {
        using var context = CreateContext();

        var existingTask = new TaskItem
        {
            Title = "Чуже завдання",
            Deadline = DateTime.UtcNow.AddDays(1),
            Status = "Поточне",
            Subject = "БД",
            UserId = 2,
        };

        context.Tasks.Add(existingTask);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var updatedTask = new TaskItem
        {
            Id = existingTask.Id,
            Title = "Нова назва",
            Deadline = DateTime.UtcNow.AddDays(2),
            Status = "Виконане",
            Subject = "БД",
        };

        var result = await service.UpdateTaskAsync(updatedTask, 1);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteTaskAsync_Should_Delete_Task_When_Owner_Matches()
    {
        using var context = CreateContext();

        var task = new TaskItem
        {
            Title = "Видалити задачу",
            Deadline = DateTime.UtcNow.AddDays(1),
            Status = "Поточне",
            Subject = "Алгоритми",
            UserId = 1,
        };

        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.DeleteTaskAsync(task.Id, 1);

        Assert.True(result.Success);
        Assert.Empty(context.Tasks);
    }

    [Fact]
    public async Task ChangeStatusAsync_Should_Change_Status()
    {
        using var context = CreateContext();

        var task = new TaskItem
        {
            Title = "Тестовий статус",
            Deadline = DateTime.UtcNow.AddDays(1),
            Status = "Поточне",
            Subject = "Web",
            UserId = 1,
        };

        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.ChangeStatusAsync(task.Id, 1, "Виконане");

        Assert.True(result.Success);

        var updatedTask = await context.Tasks.FirstAsync();
        Assert.Equal("Виконане", updatedTask.Status);
    }

    [Fact]
    public async Task GetUserTasksAsync_Should_Filter_By_Status()
    {
        using var context = CreateContext();

        context.Tasks.AddRange(
            new TaskItem
            {
                Title = "Поточне завдання",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "Програмування",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Виконане завдання",
                Deadline = DateTime.UtcNow.AddDays(-1),
                Status = "Виконане",
                Subject = "Програмування",
                UserId = 1,
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetUserTasksAsync(1, status: "Поточне");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("Поточне завдання", result.Value[0].Title);
        Assert.Equal("Поточне", result.Value[0].Status);
    }

    [Fact]
    public async Task GetUserTasksAsync_Should_Filter_By_Subject()
    {
        using var context = CreateContext();

        context.Tasks.AddRange(
            new TaskItem
            {
                Title = "Завдання з Web",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "Web",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Завдання з БД",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "БД",
                UserId = 1,
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetUserTasksAsync(1, subject: "Web");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("Завдання з Web", result.Value[0].Title);
        Assert.Equal("Web", result.Value[0].Subject);
    }

    [Fact]
    public async Task GetUserTasksAsync_Should_Search_By_Title()
    {
        using var context = CreateContext();

        context.Tasks.AddRange(
            new TaskItem
            {
                Title = "Реалізувати функцію",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "Програмування",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Прочитати конспект",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "Математика",
                UserId = 1,
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetUserTasksAsync(1);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, t => t.Title.Contains("функцію"));
    }

    [Fact]
    public async Task GetUserTasksAsync_Should_Search_By_Description()
    {
        using var context = CreateContext();

        context.Tasks.AddRange(
            new TaskItem
            {
                Title = "Завдання 1",
                Description = "Реалізувати API endpoint",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "Web",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Завдання 2",
                Description = "Написати тест",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "QA",
                UserId = 1,
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetUserTasksAsync(1);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, t => t.Description != null && t.Description.Contains("API"));
    }

    [Fact]
    public async Task GetUserTasksAsync_Should_Filter_By_Status_And_Description()
    {
        using var context = CreateContext();

        context.Tasks.AddRange(
            new TaskItem
            {
                Title = "Поточне завдання Web",
                Description = "Потрібно реалізувати endpoint",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "Web",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Виконане завдання Web",
                Description = "Endpoint реалізовано",
                Deadline = DateTime.UtcNow.AddDays(-1),
                Status = "Виконане",
                Subject = "Web",
                UserId = 1,
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetUserTasksAsync(1, status: "Поточне");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("Поточне", result.Value[0].Status);
        Assert.Contains("endpoint", result.Value[0].Description!.ToLower());
    }

    [Fact]
    public async Task GetUserTasksAsync_Should_Filter_By_Status_And_Subject()
    {
        using var context = CreateContext();

        context.Tasks.AddRange(
            new TaskItem
            {
                Title = "Поточне завдання Web",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "Web",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Поточне завдання БД",
                Deadline = DateTime.UtcNow.AddDays(1),
                Status = "Поточне",
                Subject = "БД",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Виконане завдання Web",
                Deadline = DateTime.UtcNow.AddDays(-1),
                Status = "Виконане",
                Subject = "Web",
                UserId = 1,
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetUserTasksAsync(1, status: "Поточне", subject: "Web");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("Поточне завдання Web", result.Value[0].Title);
        Assert.Equal("Поточне", result.Value[0].Status);
        Assert.Equal("Web", result.Value[0].Subject);
    }

    [Fact]
    public async Task GetTasksDueSoonAsync_Should_Return_Only_NotCompleted_User_Tasks_Within_24_Hours()
    {
        using var context = CreateContext();

        var now = new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc);

        context.Tasks.AddRange(
            new TaskItem
            {
                Title = "Завдання через 3 години",
                Deadline = now.AddHours(3),
                Status = "Поточне",
                Subject = "Програмування",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Завдання рівно через 24 години",
                Deadline = now.AddHours(24),
                Status = "Поточне",
                Subject = "Бази даних",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Завдання через 25 годин",
                Deadline = now.AddHours(25),
                Status = "Поточне",
                Subject = "Математика",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Виконане завдання через 2 години",
                Deadline = now.AddHours(2),
                Status = "Виконане",
                Subject = "Web",
                UserId = 1,
            },
            new TaskItem
            {
                Title = "Чуже завдання через 1 годину",
                Deadline = now.AddHours(1),
                Status = "Поточне",
                Subject = "Web",
                UserId = 2,
            },
            new TaskItem
            {
                Title = "Прострочене завдання",
                Deadline = now.AddHours(-1),
                Status = "Прострочене",
                Subject = "Алгоритми",
                UserId = 1,
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetTasksDueSoonAsync(1, now);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);

        Assert.Equal("Завдання через 3 години", result.Value[0].Title);
        Assert.Equal("Завдання рівно через 24 години", result.Value[1].Title);

        Assert.DoesNotContain(result.Value, t => t.Status == "Виконане");
        Assert.DoesNotContain(result.Value, t => t.UserId != 1);
        Assert.DoesNotContain(result.Value, t => t.Deadline <= now);
        Assert.DoesNotContain(result.Value, t => t.Deadline > now.AddHours(24));
    }
}