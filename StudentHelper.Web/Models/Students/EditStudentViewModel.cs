using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Students
{
    public class EditStudentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введіть ім'я.")]
        [Display(Name = "Ім'я")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть прізвище.")]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть логін.")]
        [Display(Name = "Логін")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть електронну пошту.")]
        [EmailAddress(ErrorMessage = "Некоректна електронна пошта.")]
        [Display(Name = "Електронна пошта")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Група")]
        public int? GroupId { get; set; } // int? дозволяє залишити студента без групи

        [StringLength(100, ErrorMessage = "Назва групи не може перевищувати 100 символів.")]
        [Display(Name = "Нова група (введіть або залиште пустим)")]
        public string? GroupName { get; set; }
    }
}