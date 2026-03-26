using System.Net;
using System.Text.Json;

namespace StudentHelper.Web.Middleware;

/// <summary>
/// Global exception handler middleware для обробки всіх вишків у додатку.
/// Це дозволяє позбутися try-catch блоків з контролерів та сервісів.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Немає прав доступу до цього ресурсу";
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "Ресурс не знайдено";
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = argEx.Message ?? "Некоректний аргумент";
                break;

            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message ?? "Некоректна операція";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "При обробці запиту сталась помилка. Спробуйте ще раз.";
                break;
        }

        var logger = context.RequestServices.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
        logger.LogError(exception, "Необроблений виняток: {ExceptionType} - {ExceptionMessage}", 
            exception.GetType().Name, exception.Message);

        response.TraceId = context.TraceIdentifier;

        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// DTO для відповіді на помилку
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;
}
