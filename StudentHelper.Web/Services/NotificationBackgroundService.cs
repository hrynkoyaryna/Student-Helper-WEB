using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Web.Hubs;

namespace StudentHelper.Web.Services;

/// <summary>
/// BackgroundService для перевірки подій в базі даних і надсилання нотифікацій
/// Перевіряє:
/// 1. Наближення терміну іспиту (за 24 години)
/// 2. Наближення дедлайну завдання (за 12 годин)
/// 3. Дні народження студентів у групі
/// </summary>
public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    private TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // default

    public NotificationBackgroundService(IServiceProvider serviceProvider, 
        ILogger<NotificationBackgroundService> logger,
        IHubContext<NotificationHub> hubContext,
        IOptions<ApplicationSettings> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hubContext = hubContext;

        try
        {
            var seconds = options?.Value?.NotificationCheckIntervalSeconds ?? 300;
            if (seconds < 1) seconds = 300;
            _checkInterval = TimeSpan.FromSeconds(seconds);
        }
        catch
        {
            _checkInterval = TimeSpan.FromMinutes(5);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationBackgroundService запустився");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckEventsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка в NotificationBackgroundService");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("NotificationBackgroundService зупинився");
    }

    private async Task CheckEventsAsync()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<StudentHelperDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // 1. Перевірка наближення терміну іспиту (за 24 години)
            await CheckUpcomingExamsAsync(context, notificationService);

            // 2. Перевірка наближення дедлайну завдання (за 12 годин)
            await CheckUpcomingTasksAsync(context, notificationService);

            // 3. Перевірка нових оголошень/завдань (нові завдання для групи)
            await CheckNewTasksAnnouncementsAsync(context, notificationService);
        }
    }

    /// <summary>
    /// Перевіряє наближення терміну іспитів (за 24 години)
    /// </summary>
    private async Task CheckUpcomingExamsAsync(StudentHelperDbContext context, INotificationService notificationService)
    {
        try
        {
            var now = DateTime.UtcNow;
            var examCheckWindow = now.AddHours(24);

            // Отримуємо всі іспити, які відбудуться протягом наступних 24 годин
            var upcomingExams = await context.Exams
                .Where(e => e.DateTime > now && e.DateTime <= examCheckWindow)
                .ToListAsync();

            foreach (var exam in upcomingExams)
            {
                // Якщо іспит прив'язаний до групи, надсилаємо нотифікації всім учасникам групи
                if (exam.GroupId.HasValue)
                {
                    var groupUsers = await context.Users
                        .Where(u => u.GroupId == exam.GroupId)
                        .ToListAsync();

                    foreach (var user in groupUsers)
                    {
                        var existingNotification = await context.Notifications
                            .FirstOrDefaultAsync(n => n.UserId == user.Id &&
                                n.Type == "exam_upcoming" &&
                                n.RelatedEntityId == exam.Id.ToString());

                        if (existingNotification != null)
                            continue;

                        var hoursRemaining = (int)Math.Ceiling((exam.DateTime - now).TotalHours);
                        var message = $"До іспиту з предмету '{exam.Subject}' залишилося {hoursRemaining} годин(и). " +
                            $"Дата та час: {exam.DateTime:dd.MM.yyyy HH:mm}";

                        await notificationService.CreateNotificationAsync(
                            userId: user.Id,
                            title: "Наближується іспит!",
                            message: message,
                            type: "exam_upcoming",
                            relatedEntityId: exam.Id.ToString(),
                            icon: "bi-hourglass-end",
                            actionUrl: "/Exams/Index"
                        );

                        _logger.LogInformation($"Нотифікація про наближення іспиту надіслана користувачу {user.Id} (груповий іспит)");

                        await _hubContext.Clients.Group($"user-{user.Id}")
                            .SendAsync("ReceiveNotification", new Dictionary<string, object>
                            {
                                ["title"] = "Наближується іспит!",
                                ["message"] = message,
                                ["type"] = "exam_upcoming",
                                ["icon"] = "bi-hourglass-end",
                                ["timestamp"] = DateTime.UtcNow
                            });
                    }
                }
                else
                {
                    // Іспит не прив'язаний до групи — надсилаємо нотифікацію власнику/автору
                    var existingNotification = await context.Notifications
                        .FirstOrDefaultAsync(n => n.UserId == exam.UserId &&
                            n.Type == "exam_upcoming" &&
                            n.RelatedEntityId == exam.Id.ToString());

                    if (existingNotification == null)
                    {
                        var hoursRemaining = (int)Math.Ceiling((exam.DateTime - now).TotalHours);
                        var message = $"До іспиту з предмету '{exam.Subject}' залишилося {hoursRemaining} годин(и). " +
                            $"Дата та час: {exam.DateTime:dd.MM.yyyy HH:mm}";

                        await notificationService.CreateNotificationAsync(
                            userId: exam.UserId,
                            title: "Наближується іспит!",
                            message: message,
                            type: "exam_upcoming",
                            relatedEntityId: exam.Id.ToString(),
                            icon: "bi-hourglass-end",
                            actionUrl: "/Exams/Index"
                        );

                        _logger.LogInformation($"Нотифікація про наближення іспиту надіслана користувачу {exam.UserId}");

                        // Надсилаємо SignalR повідомлення
                        await _hubContext.Clients.Group($"user-{exam.UserId}")
                            .SendAsync("ReceiveNotification", new Dictionary<string, object>
                            {
                                ["title"] = "Наближується іспит!",
                                ["message"] = message,
                                ["type"] = "exam_upcoming",
                                ["icon"] = "bi-hourglass-end",
                                ["timestamp"] = DateTime.UtcNow
                            });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці наближення іспитів");
        }
    }

    /// <summary>
    /// Перевіряє наближення дедлайну завдань (за 12 годин)
    /// </summary>
    private async Task CheckUpcomingTasksAsync(StudentHelperDbContext context, INotificationService notificationService)
    {
        try
        {
            var now = DateTime.UtcNow;
            var taskCheckWindow = now.AddHours(12);

            // Отримуємо всі завдання зі статусом "Pending" або "In Progress", дедлайн яких наближується
            var upcomingTasks = await context.Tasks
                .Where(t => t.Deadline > now && t.Deadline <= taskCheckWindow &&
                    (t.Status == "Pending" || t.Status == "In Progress"))
                .ToListAsync();

            foreach (var task in upcomingTasks)
            {
                // Перевіряємо, чи вже надіслана нотифікація для цього завдання
                var existingNotification = await context.Notifications
                    .FirstOrDefaultAsync(n => n.UserId == task.UserId &&
                        n.Type == "task_deadline" &&
                        n.RelatedEntityId == task.Id.ToString());

                if (existingNotification == null)
                {
                    var hoursRemaining = (int)Math.Ceiling((task.Deadline - now).TotalHours);
                    var message = $"До дедлайну завдання \"{task.Title}\" з предмету '{task.Subject}' залишилося {hoursRemaining} годин(и). " +
                        $"Дедлайн: {task.Deadline:dd.MM.yyyy HH:mm}";

                    await notificationService.CreateNotificationAsync(
                        userId: task.UserId,
                        title: "Наближується дедлайн завдання!",
                        message: message,
                        type: "task_deadline",
                        relatedEntityId: task.Id.ToString(),
                        icon: "bi-calendar-check",
                        actionUrl: "/Tasks/Index"
                    );

                    _logger.LogInformation($"Нотифікація про наближення дедлайну завдання надіслана користувачу {task.UserId}");

                    // Надсилаємо SignalR повідомлення
                    await _hubContext.Clients.Group($"user-{task.UserId}")
                        .SendAsync("ReceiveNotification", new Dictionary<string, object>
                        {
                            ["title"] = "Наближується дедлайн завдання!",
                            ["message"] = message,
                            ["type"] = "task_deadline",
                            ["icon"] = "bi-calendar-check",
                            ["timestamp"] = DateTime.UtcNow
                        });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці наближення дедлайну завдань");
        }
    }

    /// <summary>
    /// Перевіряє нові оголошення/завдання, створені за останній інтервал перевірки
    /// </summary>
    private async Task CheckNewTasksAnnouncementsAsync(StudentHelperDbContext context, INotificationService notificationService)
    {
        try
        {
            var now = DateTime.UtcNow;
            var windowStart = now - _checkInterval; // перевіряємо задачі створені в інтервалі

            // Отримуємо всі завдання, створені в останню перевірку
            var newTasks = await context.Tasks
                .Where(t => t.CreatedAt >= windowStart && t.CreatedAt <= now)
                .ToListAsync();

            foreach (var task in newTasks)
            {
                // Пошук користувача та його групи
                var user = await context.Users.FindAsync(task.UserId);
                if (user == null) continue;

                if (user.GroupId.HasValue)
                {
                    var groupUsers = await context.Users
                        .Where(u => u.GroupId == user.GroupId && u.Id != task.UserId)
                        .ToListAsync();

                    if (!groupUsers.Any()) continue;

                    foreach (var member in groupUsers)
                    {
                        // Перевіряємо, чи вже була нотифікація для цього завдання
                        var existingNotification = await context.Notifications
                            .FirstOrDefaultAsync(n => n.UserId == member.Id && n.Type == "task_new_announcement" && n.RelatedEntityId == task.Id.ToString());

                        if (existingNotification != null)
                            continue;

                        var message = $"Нове завдання: \"{task.Title}\" для предмету '{task.Subject}'. Дедлайн: {task.Deadline:dd.MM.yyyy HH:mm}";

                        await notificationService.CreateNotificationAsync(
                            userId: member.Id,
                            title: "Нове оголошення / завдання",
                            message: message,
                            type: "task_new_announcement",
                            relatedEntityId: task.Id.ToString(),
                            icon: "bi-megaphone",
                            actionUrl: "/Tasks/Index"
                        );

                        _logger.LogInformation($"Нове оголошення: нотифікація надіслана користувачу {member.Id} про завдання {task.Id}");

                        await _hubContext.Clients.Group($"user-{member.Id}")
                            .SendAsync("ReceiveNotification", new Dictionary<string, object>
                            {
                                ["title"] = "Нове оголошення / завдання",
                                ["message"] = message,
                                ["type"] = "task_new_announcement",
                                ["icon"] = "bi-megaphone",
                                ["timestamp"] = DateTime.UtcNow
                            });
                    }
                }
                else
                {
                    // Якщо автор не у групі, можна повідомити його особисто або пропустити
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці нових оголошень/завдань");
        }
    }
}
