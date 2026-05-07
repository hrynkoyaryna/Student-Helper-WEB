using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace StudentHelper.Web.Hubs;

/// <summary>
/// SignalR Hub для отримання нотифікацій користувачами в режимі реального часу
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            _logger.LogInformation($"Користувач {userId} підключився до NotificationHub. ConnectionId: {Context.ConnectionId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (userId != null)
        {
            _logger.LogInformation($"Користувач {userId} відключився від NotificationHub. ConnectionId: {Context.ConnectionId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
