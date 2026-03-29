namespace StudentHelper.Domain.Entities;

public class PersonalEvent
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string Color { get; set; } = "#5bc8d8"; 
}