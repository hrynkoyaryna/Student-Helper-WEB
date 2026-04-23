namespace StudentHelper.Domain.Entities;

public class Exam
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int TeacherId { get; set; } // Додай це
    public string? Description { get; set; } // Додай це
    public int UserId { get; set; } // Додай це
    public int? GroupId { get; set; } // Null for user-created exams, has value for admin-created group exams
    
    // Navigation properties
    public Teacher? Teacher { get; set; }
    public Group? Group { get; set; }

    // Calculated property for convenience
    public string TeacherName => Teacher?.FullName ?? string.Empty;
}
