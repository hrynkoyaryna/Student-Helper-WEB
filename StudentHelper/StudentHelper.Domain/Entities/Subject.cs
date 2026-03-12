namespace StudentHelper.Domain.Entities;

public class Subject
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public ICollection<ScheduleLesson> ScheduleLessons { get; set; } = new List<ScheduleLesson>();
}
