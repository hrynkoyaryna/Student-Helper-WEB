using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StudentHelper.Application.Interfaces;
using StudentHelper.Web.Hubs;

namespace StudentHelper.Web.Controllers;

public class NotificationsController : BaseController
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger, IHubContext<NotificationHub> hubContext)
    {
        _notificationService = notificationService;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Отримує непрочитані нотифікації користувача
    /// </summary>
    [HttpGet("api/notifications/unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var userId = GetCurrentUserId();
        var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
        return Ok(notifications);
    }

    /// <summary>
    /// Отримує всі нотифікації користувача
    /// </summary>
    [HttpGet("api/notifications")]
    public async Task<IActionResult> GetAllNotifications([FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        var notifications = await _notificationService.GetAllNotificationsAsync(userId, limit);
        return Ok(notifications);
    }

    /// <summary>
    /// Позначає нотифікацію як прочитану
    /// </summary>
    [HttpPost("api/notifications/{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Помилка при позначенні нотифікації {id} як прочитаної");
            return BadRequest();
        }
    }

    /// <summary>
    /// Позначає всі нотифікації користувача як прочитані
    /// </summary>
    [HttpPost("api/notifications/mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при позначенні всіх нотифікацій як прочитаних");
            return BadRequest();
        }
    }

    /// <summary>
    /// Видаляє нотифікацію
    /// </summary>
    [HttpDelete("api/notifications/{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        try
        {
            await _notificationService.DeleteNotificationAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Помилка при видаленні нотифікації {id}");
            return BadRequest();
        }
    }

    /// <summary>
    /// Отримує кількість непрочитаних нотифікацій
    /// </summary>
    [HttpGet("api/notifications/unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
        return Ok(new { unreadCount = notifications.Count });
    }

    /// <summary>
    /// Тестовий endpoint для відправки нотифікації поточному користувачу та надсилання її через SignalR
    /// </summary>
    [HttpPost("api/notifications/send-test")]
    public async Task<IActionResult> SendTestNotification()
    {
        try
        {
            var userId = GetCurrentUserId();

            var notification = await _notificationService.CreateNotificationAsync(
                userId: userId,
                title: "Тестова нотифікація",
                message: "Це тестове повідомлення від сервера",
                type: "manual",
                relatedEntityId: null,
                icon: "bi-bell",
                actionUrl: "/"
            );

            // Надсилаємо через SignalR
            await _hubContext.Clients.Group($"user-{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type,
                    icon = notification.Icon,
                    timestamp = notification.CreatedAt
                });

            return Ok(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці тестової нотифікації");
            return BadRequest();
        }
    }
}
