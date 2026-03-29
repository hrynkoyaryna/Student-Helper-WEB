namespace StudentHelper.Web.Models.Tasks;

public class TaskCreateEditViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public string Status { get; set; } = "ToDo";
    
    public string? Description { get; set; } 
}