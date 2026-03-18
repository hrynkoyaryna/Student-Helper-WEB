using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress(ErrorMessage = "Невірний формат email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}
