namespace StudentHelper.Application.Models;

public class ApplicationSettings
{
    public int ItemsPerPage { get; set; }
    public int MinSearchCharacters { get; set; }
    public int CalendarStartHour { get; set; }
    public int MaxTaskDescriptionLength { get; set; }
    public PasswordSettings PasswordSettings { get; set; } = new();
    public CacheSettings CacheSettings { get; set; } = new();
}

public class PasswordSettings
{
    public int RequiredLength { get; set; } = 8;
    public bool RequireDigit { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
}

public class CacheSettings
{
    public int GroupsSeconds { get; set; } = 3600;
    public int SubjectsSeconds { get; set; } = 3600;
    public int TeachersSeconds { get; set; } = 3600;
}