using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Models.Calendar;

public class CalendarIndexViewModel
{
    public DateOnly WeekStartDate { get; set; }

    public IReadOnlyCollection<DateOnly> Days { get; set; } = new List<DateOnly>();

    public IReadOnlyCollection<TimeOnly> TimeSlots { get; set; } = new List<TimeOnly>();

    public IReadOnlyCollection<PersonalEvent> Events { get; set; } = new List<PersonalEvent>();
}