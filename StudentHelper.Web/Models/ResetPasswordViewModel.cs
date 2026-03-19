using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models;

public class ResetPasswordViewModel
{
    public int UserId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть новий пароль")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*])[A-Za-z\d!@#$%^&*]{8,}$",
        ErrorMessage = "Пароль має містити мінімум 8 символів, велику літеру, цифру та спецсимвол (!@#$%)")]
    [Display(Name = "Новий пароль")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Підтвердіть новий пароль")]
    [Compare("Password", ErrorMessage = "Паролі не співпадають")]
    [Display(Name = "Підтвердження пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
