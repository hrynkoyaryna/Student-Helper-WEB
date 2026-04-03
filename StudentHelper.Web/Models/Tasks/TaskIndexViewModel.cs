using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Models.Tasks;

public class TaskIndexViewModel
{
    public List<TaskItem> Tasks { get; set; } = new();

    public List<string> Subjects { get; set; } = new();

    public string SelectedStatus { get; set; } = "Поточне";

    public string? SelectedSubject { get; set; }

    public string? SearchTerm { get; set; }
}