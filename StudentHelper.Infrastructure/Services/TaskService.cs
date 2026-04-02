using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Application.Models;

namespace StudentHelper.Infrastructure.Services;


public class TaskService : ITaskService
{
    private readonly StudentHelperDbContext _context;
    private readonly ILogger<TaskService> _logger;

    public TaskService(StudentHelperDbContext context, ILogger<TaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TaskItem>> GetUserTasksAsync(int userId, string? status = null, string? subject = null)
    {
        IQueryable<TaskItem> query = _context.Tasks.Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            query = query.Where(t => t.Subject == subject);
        }

        var tasks = await query
            .OrderBy(t => t.Deadline)
            .ToListAsync();

        foreach (var task in tasks)
        {
            UpdateOverdueStatusIfNeeded(task);
        }

        await _context.SaveChangesAsync();

        return tasks;
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int id, int userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task is not null)
        {
            UpdateOverdueStatusIfNeeded(task);
            await _context.SaveChangesAsync();
        }

        return task;
    }

    public async Task<Result> CreateTaskAsync(TaskItem task)
    {
        task.Deadline = NormalizeToUtc(task.Deadline);
        UpdateOverdueStatusIfNeeded(task);

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created task {TaskId} for user {UserId}", task.Id, task.UserId);
        return "Завдання успішно створено";
    }

    public async Task<Result> UpdateTaskAsync(TaskItem task, int userId)
    {
        var existingTask = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == task.Id && t.UserId == userId);

        if (existingTask is null)
        {
            _logger.LogWarning("Task {TaskId} was not found for user {UserId}", task.Id, userId);
            return Result.Fail("Завдання не знайдено");
        }

        existingTask.Title = task.Title;
        existingTask.Deadline = NormalizeToUtc(task.Deadline);
        existingTask.Status = task.Status;
        existingTask.Subject = task.Subject;
        existingTask.Description = task.Description;

        UpdateOverdueStatusIfNeeded(existingTask);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated task {TaskId}", task.Id);
        return "Завдання успішно оновлено";
    }

    public async Task<Result> DeleteTaskAsync(int id, int userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} was not found for deletion for user {UserId}", id, userId);
            return Result.Fail("Завдання не знайдено");
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted task {TaskId}", id);
        return "Завдання успішно видалено";
    }

    public async Task<Result> ChangeStatusAsync(int id, int userId, string status)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} was not found for status change for user {UserId}", id, userId);
            return Result.Fail("Завдання не знайдено");
        }

        task.Status = status;
        UpdateOverdueStatusIfNeeded(task);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Changed task {TaskId} status to {Status}", id, task.Status);
        return "Статус завдання оновлено";
    }

    public async Task<List<string>> GetUserSubjectsAsync(int userId)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId && !string.IsNullOrWhiteSpace(t.Subject))
            .Select(t => t.Subject)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    private static void UpdateOverdueStatusIfNeeded(TaskItem task)
    {
        if (task.Status != "Виконане" && task.Deadline < DateTime.UtcNow)
        {
            task.Status = "Прострочене";
        }
    }
    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
