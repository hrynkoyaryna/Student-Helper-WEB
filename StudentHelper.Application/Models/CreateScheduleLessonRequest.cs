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

    public string Recurrence { get; set; } = string.Empty;

    public string? Place { get; set; }
}
