using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Settings;

public class ChangePasswordViewModel
{
    [Display(Name = "Поточний пароль")]
    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Поточний пароль є обов'язковим")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Display(Name = "Новий пароль")]
    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Новий пароль є обов'язковим")]
    [StringLength(100, ErrorMessage = "{0} повинен мати мінімум {2} і максимум {1} символи.", MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Підтвердити пароль")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Паролі не збігаються")]
    [Required(ErrorMessage = "Підтвердження пароля є обов'язковим")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
