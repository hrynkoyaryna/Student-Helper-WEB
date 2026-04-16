using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface ITaskService
{
    Task<Result<List<TaskItem>>> GetUserTasksAsync(
        int userId,
        string? status = null,
        string? subject = null,
        string? searchTerm = null,
        int pageNumber = 1);

    Task<Result<List<TaskItem>>> GetAllUserTasksAsync(
        int userId,
        string? status = null,
        string? subject = null,
        string? searchTerm = null);

    Task<int> GetUserTasksCountAsync(
        int userId,
        string? status = null,
        string? subject = null,
        string? searchTerm = null);

    Task<Result<TaskItem>> GetTaskByIdAsync(int id, int userId);

    Task<Result> CreateTaskAsync(TaskItem task);

    Task<Result> UpdateTaskAsync(TaskItem task, int userId);

    Task<Result> DeleteTaskAsync(int id, int userId);

    Task<Result> ChangeStatusAsync(int id, int userId, string status);

    Task<Result<List<string>>> GetUserSubjectsAsync(int userId);
}
