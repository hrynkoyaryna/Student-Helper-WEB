namespace StudentHelper.Application.Models;

public class ApplicationSettings
{
    public int ItemsPerPage { get; set; }
    public int MinSearchCharacters { get; set; }
    public int CalendarStartHour { get; set; }
    public int MaxTaskDescriptionLength { get; set; }
}