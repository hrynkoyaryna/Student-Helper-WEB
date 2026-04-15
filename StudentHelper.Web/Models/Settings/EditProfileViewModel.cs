using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Settings;

public class EditProfileViewModel
{
    [Required(ErrorMessage = "Ім'я є обов'язковим")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Прізвище є обов'язковим")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Email є обов'язковим")]
    [EmailAddress(ErrorMessage = "Некоректний формат Email")]
    public string Email { get; set; }
}