using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Calendar;

namespace StudentHelper.Web.Models.Schedule;

public class GroupScheduleViewModel
{
    public List<Group> Groups { get; set; } = new();
    public int SelectedGroupId { get; set; }

    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate => WeekStartDate.AddDays(6);
    public List<DateOnly> Days { get; set; } = new();
    public List<TimeOnly> TimeSlots { get; set; } = new();
    public List<CalendarEventViewModel> Events { get; set; } = new();
}