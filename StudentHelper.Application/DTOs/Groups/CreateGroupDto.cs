using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Application.DTOs.Groups;

public class CreateGroupDto
{
    [Required(ErrorMessage = "Назва групи є обов’язковою.")]
    [StringLength(50, ErrorMessage = "Назва групи не може перевищувати 50 символів.")]
    public string Name { get; set; } = string.Empty;
}