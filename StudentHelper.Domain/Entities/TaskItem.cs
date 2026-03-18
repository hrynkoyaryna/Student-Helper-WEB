namespace StudentHelper.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime Deadline { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
