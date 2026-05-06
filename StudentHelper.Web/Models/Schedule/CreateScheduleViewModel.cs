using System.ComponentModel.DataAnnotations;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Models.Schedule;

public class CreateScheduleViewModel
{
    [Required(ErrorMessage = "Поле 'Дата' є обов'язковим")]
    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Поле 'Початок' є обов'язковим")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Поле 'Кінець' є обов'язковим")]
    public TimeOnly EndTime { get; set; }

    // Робимо nullable int?, щоб форма могла відправляти пусте значення при ручному введенні назви
    public int? SubjectId { get; set; }

    public string? SubjectTitle { get; set; }

    public int? TeacherId { get; set; }

    public string? TeacherName { get; set; }

    // Теж nullable int?, щоб працювало введення нової групи
    public int? GroupId { get; set; }
    
    public string? GroupName { get; set; }

    public string? Type { get; set; }

    public string? Recurrence { get; set; }

    public string? RecurrenceType { get; set; }
    public DateOnly? RecurrenceUntil { get; set; }

    public string? Place { get; set; }

    public List<Group> Groups { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
    public List<Teacher> Teachers { get; set; } = new();
}