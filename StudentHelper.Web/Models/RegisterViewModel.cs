using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введіть ім'я")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть прізвище")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress(ErrorMessage = "Невірний формат email")]
    [RegularExpression(@"^[\w\.-]+@[\w\.-]+\.\w{2,}$", ErrorMessage = "Введіть коректний email (наприклад: email@lnu.edu.ua)")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть пароль")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*])[A-Za-z\d!@#$%^&*]{8,}$",
        ErrorMessage = "Пароль має містити мінімум 8 символів, велику літеру, цифру та спецсимвол (!@#$%)")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Підтвердіть пароль")]
    [Compare("Password", ErrorMessage = "Паролі не співпадають")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
