using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Models.Exams;

public class ExamIndexViewModel
{
    public List<Exam> Exams { get; set; } = new();
    public List<string> Subjects { get; set; } = new();
    public string? SelectedSubject { get; set; }
    public string TimeFilter { get; set; } = "all"; 
    public string SortOrder { get; set; } = "subject_asc"; 
}
