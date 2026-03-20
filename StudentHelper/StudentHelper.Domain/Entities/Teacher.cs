namespace StudentHelper.Domain.Entities;

public class Teacher
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public ICollection<ScheduleLesson> ScheduleLessons { get; set; } = new List<ScheduleLesson>();
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}
