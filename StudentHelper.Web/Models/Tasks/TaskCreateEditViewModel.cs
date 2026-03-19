using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Tasks;

public class TaskCreateEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Введіть назву завдання")]
    [StringLength(100, ErrorMessage = "Назва не може бути довшою за 100 символів")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть дату і час дедлайну")]
    public DateTime Deadline { get; set; }

    [Required(ErrorMessage = "Оберіть статус")]
    public string Status { get; set; } = "Поточне";

    [Required(ErrorMessage = "Введіть предмет")]
    [StringLength(100, ErrorMessage = "Назва предмета не може бути довшою за 100 символів")]
    public string Subject { get; set; } = string.Empty;
}