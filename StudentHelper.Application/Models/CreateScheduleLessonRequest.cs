using System;

namespace StudentHelper.Application.Models;

public class CreateScheduleLessonRequest
{
    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int SubjectId { get; set; }

    // Allow specifying subject title alternatively (if front-end uses free text)
    public string? SubjectTitle { get; set; }

    public int? TeacherId { get; set; }

    public string? TeacherName { get; set; }

    public int GroupId { get; set; }

    public string Type { get; set; } = "Lecture";

    // Legacy free-form recurrence string (kept for compatibility)
    public string Recurrence { get; set; } = string.Empty;

    // New structured recurrence: None, Daily, Weekly, BiWeekly
    public string? RecurrenceType { get; set; }

    // Optional end date for recurrence
    public DateOnly? RecurrenceUntil { get; set; }

    public string? Place { get; set; }
}
