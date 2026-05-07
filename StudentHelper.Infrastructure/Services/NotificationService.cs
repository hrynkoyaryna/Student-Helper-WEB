using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Services;

/// <summary>
/// Сервіс для управління нотифікаціями.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly StudentHelperDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(StudentHelperDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<NotificationModel>> GetUnreadNotificationsAsync(int userId)
    {
        try
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Помилка при отриманні непрочитаних нотифікацій для користувача {userId}");
            return new List<NotificationModel>();
        }
    }

    public async Task<List<NotificationModel>> GetAllNotificationsAsync(int userId, int limit = 50)
    {
        try
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Помилка при отриманні нотифікацій для користувача {userId}");
            return new List<NotificationModel>();
        }
    }

    public async Task<NotificationModel> CreateNotificationAsync(int userId, string title, string message, string type,
        string? relatedEntityId = null, string? icon = null, string? actionUrl = null)
    {
        try
        {
            var notification = new NotificationModel
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntityId = relatedEntityId,
                Icon = icon,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Нотифікація типу '{type}' створена для користувача {userId}");

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Помилка при створенні нотифікації для користувача {userId}");
            throw;
        }
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Помилка при позначенні нотифікації {notificationId} як прочитаної");
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Помилка при позначенні всіх нотифікацій користувача {userId} як прочитаних");
        }
    }

    public async Task DeleteNotificationAsync(int notificationId)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Помилка при видаленні нотифікації {notificationId}");
        }
    }

    public async Task DeleteOldNotificationsAsync()
    {
        try
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var oldNotifications = await _context.Notifications
                .Where(n => n.CreatedAt < thirtyDaysAgo)
                .ToListAsync();

            _context.Notifications.RemoveRange(oldNotifications);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Видалено {oldNotifications.Count} старих нотифікацій");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при видаленні старих нотифікацій");
        }
    }
}
