namespace StudentHelper.Domain.Entities;

public class ScheduleLesson
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Recurrence { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
}
