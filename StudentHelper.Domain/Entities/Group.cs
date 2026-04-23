namespace StudentHelper.Domain.Entities;

public class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<ScheduleLesson> ScheduleLessons { get; set; } = new List<ScheduleLesson>();
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}
