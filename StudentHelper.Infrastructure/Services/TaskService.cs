using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly StudentHelperDbContext _context;
    private readonly ILogger<TaskService> _logger;
    private readonly IOptions<ApplicationSettings> _settings;

    public TaskService(StudentHelperDbContext context, ILogger<TaskService> logger, IOptions<ApplicationSettings> settings)
    {
        _context = context;
        _logger = logger;
        _settings = settings;
    }

    public async Task<Result<List<TaskItem>>> GetUserTasksAsync(
        int userId,
        string? status = null,
        string? subject = null,
        string? searchTerm = null,
        int pageNumber = 1)
    {
        IQueryable<TaskItem> query = _context.Tasks
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            query = query.Where(t => t.Subject == subject);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var minSearchChars = _settings.Value.MinSearchCharacters;

            if (searchTerm.Length >= minSearchChars)
            {
                query = query.Where(t =>
                    EF.Functions.ILike(t.Title, $"%{searchTerm}%") ||
                    EF.Functions.ILike(t.Description!, $"%{searchTerm}%") ||
                    EF.Functions.ILike(t.Subject!, $"%{searchTerm}%"));
            }
            else
            {
                _logger.LogWarning("Search query '{SearchTerm}' is too short. Minimum: {MinChars}", searchTerm, minSearchChars);
            }
        }

        var itemsPerPage = _settings.Value.ItemsPerPage;
        var skipCount = (pageNumber - 1) * itemsPerPage;

        var tasks = await query
            .OrderBy(t => t.Deadline)
            .Skip(skipCount)
            .Take(itemsPerPage)
            .ToListAsync();

        foreach (var task in tasks)
        {
            UpdateOverdueStatusIfNeeded(task);
        }

        await _context.SaveChangesAsync();

        return tasks;
    }

    public async Task<int> GetUserTasksCountAsync(int userId, string? status = null, string? subject = null, string? searchTerm = null)
    {
        IQueryable<TaskItem> query = _context.Tasks
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            query = query.Where(t => t.Subject == subject);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var minSearchChars = _settings.Value.MinSearchCharacters;

            if (searchTerm.Length >= minSearchChars)
            {
                query = query.Where(t =>
                    EF.Functions.ILike(t.Title, $"%{searchTerm}%") ||
                    EF.Functions.ILike(t.Description!, $"%{searchTerm}%") ||
                    EF.Functions.ILike(t.Subject!, $"%{searchTerm}%"));
            }
        }

        return await query.CountAsync();
    }

    public async Task<Result<List<TaskItem>>> GetAllUserTasksAsync(
        int userId,
        string? status = null,
        string? subject = null,
        string? searchTerm = null)
    {
        IQueryable<TaskItem> query = _context.Tasks
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            query = query.Where(t => t.Subject == subject);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var minSearchChars = _settings.Value.MinSearchCharacters;

            if (searchTerm.Length >= minSearchChars)
            {
                query = query.Where(t =>
                    EF.Functions.ILike(t.Title, $"%{searchTerm}%") ||
                    EF.Functions.ILike(t.Description!, $"%{searchTerm}%") ||
                    EF.Functions.ILike(t.Subject!, $"%{searchTerm}%"));
            }
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

    public async Task<Result<TaskItem>> GetTaskByIdAsync(int id, int userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} was not found for user {UserId}", id, userId);
            return Result<TaskItem>.Fail("Завдання не знайдено");
        }

        UpdateOverdueStatusIfNeeded(task);
        await _context.SaveChangesAsync();

        return task;
    }

    public async Task<Result> CreateTaskAsync(TaskItem task)
    {
        var maxLength = _settings.Value.MaxTaskDescriptionLength;

        if (!string.IsNullOrEmpty(task.Description) && task.Description.Length > maxLength)
        {
            _logger.LogWarning("Task description too long for user {UserId}", task.UserId);
            return Result.Fail($"Опис не може перевищувати {maxLength} символів");
        }

        task.Deadline = NormalizeToUtc(task.Deadline);
        UpdateOverdueStatusIfNeeded(task);

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created task {TaskId} for user {UserId}", task.Id, task.UserId);
        return "Завдання успішно створено";
    }

    public async Task<Result> UpdateTaskAsync(TaskItem task, int userId)
    {
        var maxLength = _settings.Value.MaxTaskDescriptionLength;

        if (!string.IsNullOrEmpty(task.Description) && task.Description.Length > maxLength)
        {
            _logger.LogWarning("Task description too long for user {UserId}", userId);
            return Result.Fail($"Опис не може перевищувати {maxLength} символів");
        }

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

    public async Task<Result<List<TaskItem>>> GetTasksDueSoonAsync(int userId, DateTime currentTimeUtc)
    {
        currentTimeUtc = NormalizeToUtc(currentTimeUtc);
        var dueUntilUtc = currentTimeUtc.AddHours(24);

        var tasks = await _context.Tasks
            .Where(t =>
                t.UserId == userId &&
                t.Status != "Виконане" &&
                t.Deadline > currentTimeUtc &&
                t.Deadline <= dueUntilUtc)
            .OrderBy(t => t.Deadline)
            .ToListAsync();

        return tasks;
    }

    public async Task<Result<List<string>>> GetUserSubjectsAsync(int userId)
    {
        var subjects = await _context.Tasks
            .Where(t => t.UserId == userId && !string.IsNullOrWhiteSpace(t.Subject))
            .Select(t => t.Subject!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        return subjects;
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