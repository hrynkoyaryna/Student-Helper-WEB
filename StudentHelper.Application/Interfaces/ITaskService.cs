using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface ITaskService
{
	Task<List<TaskItem>> GetUserTasksAsync(int userId, string? status = null, string? subject = null);
	Task<TaskItem?> GetTaskByIdAsync(int id, int userId);
	Task CreateTaskAsync(TaskItem task);
	Task<bool> UpdateTaskAsync(TaskItem task, int userId);
	Task<bool> DeleteTaskAsync(int id, int userId);
	Task<bool> ChangeStatusAsync(int id, int userId, string status);
	Task<List<string>> GetUserSubjectsAsync(int userId);
}