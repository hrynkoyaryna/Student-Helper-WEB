namespace StudentHelper.Domain.Entities;

public class Exam
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int TeacherId { get; set; } // Додай це
    public string? Description { get; set; } // Додай це
    public int UserId { get; set; } // Додай це
    
    // Navigation properties
    public Teacher? Teacher { get; set; }

    // Calculated property for convenience
    public string TeacherName => Teacher?.FullName ?? string.Empty;
}
