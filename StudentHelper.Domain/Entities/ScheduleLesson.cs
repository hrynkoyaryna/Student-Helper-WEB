using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentHelper.Domain.Entities
{
    public class ScheduleLesson
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int GroupId { get; set; }
        [ForeignKey("GroupId")]
        public virtual Group Group { get; set; } = null!;
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; } = null!;
        [Required]
        public int TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public virtual Teacher Teacher { get; set; } = null!;
        [Required]
        public DayOfWeek DayOfWeek { get; set; }
        [Required]
        public TimeSpan StartTime { get; set; }
        [Required]
        public TimeSpan EndTime { get; set; }
        [StringLength(50)]
        public string? Room { get; set; }
        [StringLength(100)]
        public string? LessonType { get; set; }
        public bool? IsEvenWeek { get; set; }
        public string? Type { get; set; }
    }
}
