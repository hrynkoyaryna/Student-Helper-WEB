using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface IExamsService
{
    Task<List<Exam>> GetExamsAsync();
    Task<Exam?> GetExamByIdAsync(int id);
    Task<List<Exam>> GetByUserIdAsync(int userId);

    // Existing domain-based methods (kept for compatibility)
    Task<Result> CreateExamAsync(Exam exam);
    Task<Result> UpdateExamAsync(Exam exam);
    Task<Result> DeleteExamAsync(int id);

    // New request-based methods that handle teacher lookup/creation
    Task<Result> CreateExamAsync(CreateExamRequest request);
    Task<Result> UpdateExamAsync(UpdateExamRequest request);

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
