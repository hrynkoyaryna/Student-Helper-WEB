namespace StudentHelper.Web.Models.Calendar;

public class CalendarEventViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public DateTime StartAt => Start;

    public DateTime EndAt => End;

    public string? Description { get; set; }

    public string? Color { get; set; }

    public string Type { get; set; } = "Event";
}