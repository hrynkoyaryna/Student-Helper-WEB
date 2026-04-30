using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Schedule
{
    public class CreateScheduleViewModel
    {
        [Required(ErrorMessage = "Оберіть групу")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Вкажіть назву предмета")]
        public string SubjectTitle { get; set; } = string.Empty; // Текст з клавіатури

        [Required(ErrorMessage = "Вкажіть ПІБ викладача")]
        public string TeacherFullName { get; set; } = string.Empty; // Текст з клавіатури

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(4);

        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Room { get; set; } = "Н/Д";
        public string LessonType { get; set; } = "Лекція";
        public bool? IsEvenWeek { get; set; }

        public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
    }
}