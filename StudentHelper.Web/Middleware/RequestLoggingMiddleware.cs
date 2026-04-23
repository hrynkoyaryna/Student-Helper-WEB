using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Http.Extensions;

namespace StudentHelper.Web.Middleware;

/// <summary>
/// Middleware для логування інформації про кожен HTTP запит.
/// Логує:
/// - Метод запиту (GET, POST, PUT, DELETE і т.д.)
/// - URL адресу
/// - IP адресу клієнта
/// - Хедери запиту
/// - Тіло запиту (для методів що передають дані)
/// - ID поточного користувача (якщо залогінений)
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Логуємо вхідний запит
        await LogRequestAsync(context);

        // Логуємо відповідь
        var originalBodyStream = context.Response.Body;

        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            await _next(context);

            await LogResponseAsync(context, responseBody);

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context)
    {
        var request = context.Request;

        // Отримуємо дані про запит
        var method = request.Method;
        var url = request.GetEncodedPathAndQuery();
        var ipAddress = GetClientIpAddress(context);
        var userId = GetCurrentUserId(context);

        // Читаємо тіло запиту (якщо є)
        var body = await ReadRequestBodyAsync(request);

        // Логуємо основну інформацію
        _logger.LogInformation(
            "═══════════════════════════════════════════════════════════════\n" +
            "📨 ВХІДНИЙ ЗАПИТ\n" +
            "═══════════════════════════════════════════════════════════════\n" +
            "🔹 Метод: {Method}\n" +
            "🔹 URL: {Url}\n" +
            "🔹 IP адреса: {IpAddress}\n" +
            "🔹 ID користувача: {UserId}\n" +
            "═══════════════════════════════════════════════════════════════",
            method, url, ipAddress, userId ?? "Не залогінений");

        // Логуємо хедери
        if (request.Headers.Count > 0)
        {
            var headers = new StringBuilder();
            headers.AppendLine("\n📋 ХЕДЕРИ:");
            foreach (var header in request.Headers)
            {
                // Приховуємо чутливі дані (Authorization, Cookie, Password)
                var value = ShouldMaskHeader(header.Key) ? "***MASKED***" : string.Join(", ", header.Value);
                headers.AppendLine($"   • {header.Key}: {value}");
            }
            _logger.LogInformation(headers.ToString());
        }

        // Логуємо тіло запиту (якщо є)
        if (!string.IsNullOrWhiteSpace(body))
        {
            var maskedBody = MaskSensitiveData(body, request.ContentType);
            _logger.LogInformation(
                "📦 ТІЛО ЗАПИТУ:\n{Body}",
                maskedBody);
        }

        _logger.LogInformation("═══════════════════════════════════════════════════════════════\n");
    }

    private async Task LogResponseAsync(HttpContext context, MemoryStream responseBody)
    {
        var response = context.Response;

        // Отримуємо дані про відповідь
        var statusCode = response.StatusCode;
        var body = await ReadResponseBodyAsync(responseBody);

        // Логуємо основну інформацію
        _logger.LogInformation(
            "═══════════════════════════════════════════════════════════════\n" +
            "📤 ВИХІДНА ВІДПОВІДЬ\n" +
            "═══════════════════════════════════════════════════════════════\n" +
            "🔹 Код статусу: {StatusCode}\n" +
            "═══════════════════════════════════════════════════════════════",
            statusCode);

        _logger.LogInformation("═══════════════════════════════════════════════════════════════\n");
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        // Якщо метод GET або HEAD, немає сенсу читати тіло
        if (request.Method == "GET" || request.Method == "HEAD" || request.Method == "DELETE")
        {
            return string.Empty;
        }

        // Перевіряємо чи можна прочитати тіло
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        request.Body.Position = 0;
        var body = await new StreamReader(request.Body, Encoding.UTF8).ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    private async Task<string> ReadResponseBodyAsync(MemoryStream responseBody)
    {
        responseBody.Position = 0;
        var body = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();
        responseBody.Position = 0;

        return body;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Спочатку перевіряємо заголовок X-Forwarded-For (для proxies)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var addresses = forwardedFor.ToString().Split(',');
            if (addresses.Length > 0 && !string.IsNullOrWhiteSpace(addresses[0]))
            {
                return addresses[0].Trim();
            }
        }

        // Потім перевіряємо X-Real-IP
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            if (!string.IsNullOrWhiteSpace(realIp.ToString()))
            {
                return realIp.ToString()!;
            }
        }

        // Як останній варіант, використовуємо RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString() ?? "Невідома IP";
    }

    private string? GetCurrentUserId(HttpContext context)
    {
        // Отримуємо ID користувача з Claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim?.Value;
    }

    private bool ShouldMaskHeader(string headerName)
    {
        // Список чутливих хедерів, які не слід логувати в повному вигляді
        var sensitiveHeaders = new[]
        {
            "authorization",
            "cookie",
            "x-csrf-token",
            "password",
            "x-api-key",
            "x-auth-token",
            "x-access-token"
        };

        return sensitiveHeaders.Contains(headerName.ToLower());
    }

    private string MaskSensitiveData(string body, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(body))
            return body;

        try
        {
            // Чутливі поля, які потрібно приховувати
            var sensitiveFields = new[]
            {
                "password",
                "confirmpassword",
                "currentpassword",
                "newpassword",
                "__requestverificationtoken",
                "token",
                "apikey",
                "secretkey",
                "creditcard",
                "cardnumber",
                "ssn",
                "socialSecurityNumber",
                "cvv",
                "pincode"
            };

            // Якщо це JSON
            if (contentType?.Contains("application/json") == true)
            {
                return MaskJsonFields(body, sensitiveFields);
            }

            // Якщо це URL-encoded form data
            if (contentType?.Contains("application/x-www-form-urlencoded") == true)
            {
                return MaskFormEncodedFields(body, sensitiveFields);
            }

            // Якщо це multipart/form-data
            if (contentType?.Contains("multipart/form-data") == true)
            {
                return MaskMultipartFormData(body, sensitiveFields);
            }

            return body;
        }
        catch
        {
            return body;
        }
    }

    private string MaskJsonFields(string json, string[] sensitiveFields)
    {
        try
        {
            var maskedJson = json;

            foreach (var field in sensitiveFields)
            {
                // Шукаємо паттерни типу "password": "value" або "password":"value"
                var patterns = new[]
                {
                    $@"""{field}""\s*:\s*""[^""]*""",      // "password": "value"
                    $@"""{field}""\s*:\s*'[^']*'",          // "password": 'value'
                    $@"""{field}""\s*:\s*[^,}}]*"           // "password": value (number, bool)
                };

                foreach (var pattern in patterns)
                {
                    maskedJson = Regex.Replace(
                        maskedJson,
                        pattern,
                        $"\"{field}\": \"***MASKED***\"",
                        RegexOptions.IgnoreCase);
                }
            }

            return maskedJson;
        }
        catch
        {
            return json;
        }
    }

    private string MaskFormEncodedFields(string formData, string[] sensitiveFields)
    {
        try
        {
            var pairs = formData.Split('&');
            var maskedPairs = new List<string>();

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = HttpUtility.UrlDecode(keyValue[0]);
                    var value = HttpUtility.UrlDecode(keyValue[1]);

                    if (sensitiveFields.Any(f => f.Equals(key, StringComparison.OrdinalIgnoreCase)))
                    {
                        maskedPairs.Add($"{key}=***MASKED***");
                    }
                    else
                    {
                        maskedPairs.Add(pair);
                    }
                }
                else
                {
                    maskedPairs.Add(pair);
                }
            }

            return string.Join("&", maskedPairs);
        }
        catch
        {
            return formData;
        }
    }

    private string MaskMultipartFormData(string formData, string[] sensitiveFields)
    {
        // Multipart form data - приховуємо чутливі поля
        try
        {
            var result = formData;

            foreach (var field in sensitiveFields)
            {
                // Шукаємо паттерни типу name="password" та value="xxx"
                var pattern = $@"name=""{field}""\s*\r?\n\r?\n([^\r\n-]*)";
                result = Regex.Replace(
                    result,
                    pattern,
                    $"name=\"{field}\"\r\n\r\n***MASKED***",
                    RegexOptions.IgnoreCase);
            }

            return result;
        }
        catch
        {
            return formData;
        }
    }
}

/// <summary>
/// Extension методи для реєстрації middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    /// <summary>
    /// Добавляє middleware для логування запитів до конвеєра обробки
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
