using System;

namespace StudentHelper.Application.Models
{
    public class CreateScheduleLessonRequest
    {
        public int GroupId { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectTitle { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public DateOnly Date { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? Room { get; set; }
        public string? LessonType { get; set; }
        public bool? IsEvenWeek { get; set; }
        public string? Type { get; set; }
        public string? Recurrence { get; set; }
        public string? RecurrenceType { get; set; }
        public DateOnly? RecurrenceUntil { get; set; }
        public string? Place { get; set; }
    }
}
