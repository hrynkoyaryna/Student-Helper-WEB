namespace StudentHelper.Domain.Entities;

public class Note
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool Pinned { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
