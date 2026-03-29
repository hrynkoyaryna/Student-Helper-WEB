using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Calendar;

public class CreatePersonalEventViewModel
{
    [Required(ErrorMessage = "Вкажіть назву.")]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Вкажіть дату.")]
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Вкажіть час початку.")]
    [DataType(DataType.Time)]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Вкажіть час завершення.")]
    [DataType(DataType.Time)]
    public TimeOnly EndTime { get; set; }

    [Required]
    public string Color { get; set; } = "#5bc8d8"; 
}