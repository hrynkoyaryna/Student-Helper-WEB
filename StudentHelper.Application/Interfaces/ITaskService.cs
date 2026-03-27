using StudentHelper.Domain.Entities;
using StudentHelper.Application.Models;

namespace StudentHelper.Application.Interfaces;

public interface ITaskService
{
	Task<List<TaskItem>> GetUserTasksAsync(int userId, string? status = null, string? subject = null);
	Task<TaskItem?> GetTaskByIdAsync(int id, int userId);
	Task<Result> CreateTaskAsync(TaskItem task);
	Task<Result> UpdateTaskAsync(TaskItem task, int userId);
	Task<Result> DeleteTaskAsync(int id, int userId);
	Task<Result> ChangeStatusAsync(int id, int userId, string status);
	Task<List<string>> GetUserSubjectsAsync(int userId);
}
