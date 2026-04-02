namespace StudentHelper.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public string Title { get; set; } = "Помилка";
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 500;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
