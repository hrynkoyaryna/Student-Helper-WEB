using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface IExamsService
{
    Task<List<Exam>> GetExamsAsync();
    Task<Exam?> GetExamByIdAsync(int id);
    Task<List<Exam>> GetByUserIdAsync(int userId);
    Task<List<Exam>> GetByGroupIdAsync(int groupId);

    // Existing domain-based methods (kept for compatibility)
    Task<Result> CreateExamAsync(Exam exam);
    Task<Result> UpdateExamAsync(Exam exam);

    // New request-based methods that handle teacher lookup/creation
    Task<Result> CreateExamAsync(CreateExamRequest request);
    Task<Result> UpdateExamAsync(UpdateExamRequest request);
    Task<Result> DeleteExamAsync(int id, int userId);

    // Admin methods for creating exams for groups
    Task<Result> CreateGroupExamAsync(CreateGroupExamRequest request);
    Task<Result> UpdateGroupExamAsync(UpdateGroupExamRequest request);
    
    // Permission checks
    Task<bool> CanEditExamAsync(int examId, int userId);
    Task<bool> CanDeleteExamAsync(int examId, int userId);

    // Provide teachers list so controllers do not use repositories directly
    Task<List<Teacher>> GetAllTeachersAsync();
}

public class CreateExamRequest
{
    public string Subject { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? Description { get; set; }
    public int UserId { get; set; }
}

public class UpdateExamRequest
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? Description { get; set; }
    public int UserId { get; set; }
}

public class CreateGroupExamRequest
{
    public string Subject { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? Description { get; set; }
    public int GroupId { get; set; } // Required for admin
    public int AdminUserId { get; set; }
}

public class UpdateGroupExamRequest
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? Description { get; set; }
    public int AdminUserId { get; set; }
}
