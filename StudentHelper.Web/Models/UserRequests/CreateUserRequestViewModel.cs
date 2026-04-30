using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.UserRequests;

public class CreateUserRequestViewModel
{
    [Required(ErrorMessage = "Оберіть категорію")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Оберіть тип звернення")]
    public string RequestType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть тему")]
    [StringLength(100, ErrorMessage = "Тема не може бути довшою за 100 символів")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Опишіть звернення")]
    [StringLength(1000, ErrorMessage = "Опис не може бути довшим за 1000 символів")]
    public string Description { get; set; } = string.Empty;
}