using Microsoft.AspNetCore.Identity;

namespace StudentHelper.Domain.Entities;

public class User : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<PersonalEvent> PersonalEvents { get; set; } = new List<PersonalEvent>();
}