using System;

namespace StudentHelper.Application.Models
{
    public class CreateScheduleLessonRequest
    {
        public int GroupId { get; set; }
        public int SubjectId { get; set; } 
        public int TeacherId { get; set; } 
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Room { get; set; }
        public string? LessonType { get; set; }
        public bool? IsEvenWeek { get; set; }
    }
}