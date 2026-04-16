using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Students
{
    public class AdminChangeStudentPasswordViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введіть новий пароль.")]
        [DataType(DataType.Password)]
        [Display(Name = "Новий пароль")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердіть пароль.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Паролі не збігаються.")]
        [Display(Name = "Підтвердження пароля")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}