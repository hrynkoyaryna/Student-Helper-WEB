using System.ComponentModel.DataAnnotations;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Models.Exams;

public class ExamCreateEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Введіть предмет")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть дату та час")]
    [DataType(DataType.DateTime)]
    public DateTime DateTime { get; set; }

    // The selected teacher id (optional if using existing)
    public int? TeacherId { get; set; }

    // Allow user to enter teacher full name manually
    public string? TeacherName { get; set; }

    public List<Teacher> Teachers { get; set; } = new();
}
