using System;
using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Events;

public class EventCreateViewModel
{
    [Required(ErrorMessage = "Введіть назву події")]
    [Display(Name = "Назва")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Опис")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть дату")]
    [Display(Name = "Дата")]
    [DataType(DataType.Date)]
    public DateTime EventDate { get; set; } = DateTime.Today; // По замовчуванню сьогодні

    [Required(ErrorMessage = "Вкажіть час початку")]
    [Display(Name = "Час початку")]
    public TimeSpan StartTime { get; set; }

    [Required(ErrorMessage = "Вкажіть час закінчення")]
    [Display(Name = "Час закінчення")]
    public TimeSpan EndTime { get; set; }
}