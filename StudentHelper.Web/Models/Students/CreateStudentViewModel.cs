using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Students
{
    public class CreateStudentViewModel
    {
        [Required(ErrorMessage = "Введіть логін.")]
        [Display(Name = "Логін")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть електронну пошту.")]
        [EmailAddress(ErrorMessage = "Некоректна електронна пошта.")]
        [Display(Name = "Електронна пошта")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть пароль.")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;
    }
}