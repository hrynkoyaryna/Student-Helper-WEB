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

    public int? TeacherId { get; set; }

    public string? TeacherName { get; set; }

    public string? Description { get; set; }

    public List<Teacher> Teachers { get; set; } = new();
}
