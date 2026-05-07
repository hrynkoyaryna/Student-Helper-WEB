using StudentHelper.Application.Models;

namespace StudentHelper.Application.Interfaces;

/// <summary>
/// Інтерфейс для роботи з нотифікаціями.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Отримує всі непрочитані нотифікації користувача.
    /// </summary>
    /// <param name="userId">ID користувача.</param>
    /// <returns>Список непрочитаних нотифікацій.</returns>
    Task<List<NotificationModel>> GetUnreadNotificationsAsync(int userId);

    /// <summary>
    /// Отримує всі нотифікації користувача (прочитані та непрочитані).
    /// </summary>
    /// <param name="userId">ID користувача.</param>
    /// <param name="limit">Максимальна кількість нотифікацій.</param>
    /// <returns>Список нотифікацій.</returns>
    Task<List<NotificationModel>> GetAllNotificationsAsync(int userId, int limit = 50);

    /// <summary>
    /// Створює нотифікацію для користувача.
    /// </summary>
    /// <param name="userId">ID користувача.</param>
    /// <param name="title">Заголовок нотифікації.</param>
    /// <param name="message">Текст нотифікації.</param>
    /// <param name="type">Тип нотифікації.</param>
    /// <param name="relatedEntityId">ID пов'язаної сутності.</param>
    /// <param name="icon">CSS клас для іконки.</param>
    /// <param name="actionUrl">URL для переходу.</param>
    /// <returns>Створена нотифікація.</returns>
    Task<NotificationModel> CreateNotificationAsync(int userId, string title, string message, string type, 
        string? relatedEntityId = null, string? icon = null, string? actionUrl = null);

    /// <summary>
    /// Позначає нотифікацію як прочитану.
    /// </summary>
    /// <param name="notificationId">ID нотифікації.</param>
    /// <returns>Завдання.</returns>
    Task MarkAsReadAsync(int notificationId);

    /// <summary>
    /// Позначає всі нотифікації користувача як прочитані.
    /// </summary>
    /// <param name="userId">ID користувача.</param>
    /// <returns>Завдання.</returns>
    Task MarkAllAsReadAsync(int userId);

    /// <summary>
    /// Видаляє нотифікацію.
    /// </summary>
    /// <param name="notificationId">ID нотифікації.</param>
    /// <returns>Завдання.</returns>
    Task DeleteNotificationAsync(int notificationId);

    /// <summary>
    /// Видаляє всі старі нотифікації (старші за 30 днів).
    /// </summary>
    /// <returns>Завдання.</returns>
    Task DeleteOldNotificationsAsync();
}
