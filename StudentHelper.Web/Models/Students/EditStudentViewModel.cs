using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Students
{
    public class EditStudentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введіть логін.")]
        [Display(Name = "Логін")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть електронну пошту.")]
        [EmailAddress(ErrorMessage = "Некоректна електронна пошта.")]
        [Display(Name = "Електронна пошта")]
        public string Email { get; set; } = string.Empty;
    }
}