using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Concurrent;

namespace StudentHelper.Web.Filters;

/// <summary>
/// Action фільтр для обмеження кількості запитів з однієї IP адреси.
/// Дозволяє не більше N запитів за хвилину від однієї IP.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RateLimitingFilter : Attribute, IAsyncActionFilter
{
    // Словник для зберігання інформації про запити: IP -> список часів запитів
    private static readonly ConcurrentDictionary<string, List<DateTime>> RequestLog = 
        new ConcurrentDictionary<string, List<DateTime>>();

    private readonly int _maxRequests;
    private readonly int _timeWindowSeconds;

    /// <summary>
    /// Ініціалізує фільтр з максимальною кількістю запитів за часовий проміжок.
    /// </summary>
    /// <param name="maxRequests">Максимальна кількість запитів (за замовчуванням 60)</param>
    /// <param name="timeWindowSeconds">Часовий проміжок в секундах (за замовчуванням 60)</param>
    public RateLimitingFilter(int maxRequests = 60, int timeWindowSeconds = 60)
    {
        _maxRequests = maxRequests;
        _timeWindowSeconds = timeWindowSeconds;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Отримуємо IP адресу клієнта
        var ipAddress = GetClientIpAddress(context.HttpContext);

        // Перевіряємо чи перевищено ліміт
        if (!IsRequestAllowed(ipAddress))
        {
            // Встановлюємо статус код 429 Too Many Requests
            context.Result = new RedirectToActionResult("RateLimitExceeded", "Error", null)
            {
                Permanent = false
            };
            return;
        }

        // Продовжуємо обробку запиту
        await next();
    }

    /// <summary>
    /// Отримує IP адресу клієнта з HttpContext.
    /// Враховує X-Forwarded-For заголовок для випадків за NAT/proxy.
    /// </summary>
    private string GetClientIpAddress(HttpContext context)
    {
        // Перевіряємо X-Forwarded-For заголовок (для proxy/load balancer)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Використовуємо RemoteIpAddress як fallback
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Перевіряє чи дозволений запит від цієї IP адреси.
    /// Очищує старі запити та додає новий запит до логу.
    /// </summary>
    private bool IsRequestAllowed(string ipAddress)
    {
        var now = DateTime.UtcNow;
        var timeLimit = now.AddSeconds(-_timeWindowSeconds);

        // Отримуємо або створюємо список запитів для цієї IP
        var requestTimes = RequestLog.AddOrUpdate(
            ipAddress,
            new List<DateTime> { now }, // Новий запис
            (key, existingList) =>
            {
                // Видаляємо запити старше за часовий проміжок
                existingList.RemoveAll(t => t < timeLimit);

                // Перевіряємо чи не перевищено ліміт
                if (existingList.Count >= _maxRequests)
                {
                    return existingList; // Ліміт перевищено, не додаємо новий запит
                }

                // Додаємо новий запит
                existingList.Add(now);
                return existingList;
            });

        // Ліміт перевищено, якщо в списку більше або дорівнює ліміту
        // і останній запит було додано в межах часового проміжку
        return requestTimes.Count(t => t >= timeLimit) < _maxRequests;
    }
}
