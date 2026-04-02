namespace StudentHelper.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime Deadline { get; set; }

    public string Status { get; set; } = "Pending";

    public string Subject { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}