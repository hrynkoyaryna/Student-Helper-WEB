using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Models.Schedule
{
    public class CreateScheduleViewModel
    {
        [Required(ErrorMessage = "Оберіть групу")]
        public int GroupId { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectTitle { get; set; }
        public string? TeacherFullName { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(4);
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; } = new TimeOnly(9, 0);
        public TimeOnly EndTime { get; set; } = new TimeOnly(10, 0);
        public string? Room { get; set; } = "Н/Д";
        public string? LessonType { get; set; } = "Лекція";
        public bool? IsEvenWeek { get; set; }
        public string? Type { get; set; } = "Lecture";
        public string? Recurrence { get; set; }
        public string? RecurrenceType { get; set; }
        public DateOnly? RecurrenceUntil { get; set; }
        public string? Place { get; set; }

        public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
        public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();
        public IEnumerable<Teacher> Teachers { get; set; } = new List<Teacher>();
    }
}
