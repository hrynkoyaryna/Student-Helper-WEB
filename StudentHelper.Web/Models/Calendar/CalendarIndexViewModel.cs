namespace StudentHelper.Web.Models.Calendar;

public class CalendarIndexViewModel
{
    public DateOnly WeekStartDate { get; set; }

    public List<DateOnly> Days { get; set; } = new();

    public List<TimeOnly> TimeSlots { get; set; } = new();

    public List<CalendarEventViewModel> Events { get; set; } = new();

    public DateOnly WeekEndDate => WeekStartDate.AddDays(6);
}