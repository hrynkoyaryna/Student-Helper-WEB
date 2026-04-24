using System.ComponentModel.DataAnnotations;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Models.Schedule;

public class CreateScheduleViewModel
{
    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    // Allow either selecting subject id or entering subject title
    public int SubjectId { get; set; }

    public string? SubjectTitle { get; set; }

    public int? TeacherId { get; set; }

    public string? TeacherName { get; set; }

    [Required]
    public int GroupId { get; set; }

    public string? Type { get; set; }

    public string? Recurrence { get; set; }

    public string? Place { get; set; }

    public List<Group> Groups { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
    public List<Teacher> Teachers { get; set; } = new();
}
