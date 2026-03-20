namespace StudentHelper.Domain.Entities;

public class Exam
{
    public int Id { get; set; }

    public string Subject { get; set; } = string.Empty;

    public DateTime DateTime { get; set; }

    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
}
