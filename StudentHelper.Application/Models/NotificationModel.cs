namespace StudentHelper.Application.Models;

/// <summary>
/// Модель для нотифікацій користувачів.
/// </summary>
public class NotificationModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "exam", "task", "birthday", тощо
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public string? RelatedEntityId { get; set; } // ID пов'язаної сутності (exam, task, тощо)
    public string? Icon { get; set; } // CSS клас для іконки
    public string? ActionUrl { get; set; } // URL для переходу при кліці на нотифікацію
}
