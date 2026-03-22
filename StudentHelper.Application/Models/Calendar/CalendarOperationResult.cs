namespace StudentHelper.Application.Models.Calendar;

public class CalendarOperationResult
{
    public bool Success { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;

    public static CalendarOperationResult Ok()
    {
        return new CalendarOperationResult
        {
            Success = true,
        };
    }

    public static CalendarOperationResult Fail(string errorMessage)
    {
        return new CalendarOperationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
        };
    }
}