using System.Net;
using Microsoft.AspNetCore.Http.Extensions;
using StudentHelper.Web.Models;

namespace StudentHelper.Web.Middleware;

/// <summary>
/// Global exception handler middleware для обробки всіх вишків у додатку.
/// Це дозволяє позбутися try-catch блоків з контролерів та сервісів.
/// Повертає HTML Error View вместо JSON.
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
        
        var errorViewModel = new ErrorViewModel();
        
        switch (exception)
        {
            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorViewModel.Title = "Доступ заборонено";
                errorViewModel.Message = "Немає прав доступу до цього ресурсу";
                errorViewModel.StatusCode = 401;
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                errorViewModel.Title = "Ресурс не знайдено";
                errorViewModel.Message = "Запитуваний ресурс не існує";
                errorViewModel.StatusCode = 404;
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorViewModel.Title = "Некоректний запит";
                errorViewModel.Message = argEx.Message ?? "Передані некоректні параметри";
                errorViewModel.StatusCode = 400;
                break;

            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorViewModel.Title = "Некоректна операція";
                errorViewModel.Message = exception.Message ?? "Операція не може бути виконана";
                errorViewModel.StatusCode = 400;
                break;

            case System.Net.Mail.SmtpException mailEx:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorViewModel.Title = "Помилка сервера";
                errorViewModel.Message = "Не вдалося відправити повідомлення. Спробуйте ще раз пізніше.";
                errorViewModel.StatusCode = 500;
                break;

            case IOException ioEx:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorViewModel.Title = "Помилка сервера";
                errorViewModel.Message = "Помилка при обробці файлу. Спробуйте ще раз.";
                errorViewModel.StatusCode = 500;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorViewModel.Title = "Помилка сервера";
                errorViewModel.Message = "При обробці запиту сталась помилка. Спробуйте ще раз.";
                errorViewModel.StatusCode = 500;
                break;
        }

        logger.LogError(exception, "Необроблений виняток: {ExceptionType} - {ExceptionMessage}", 
            exception.GetType().Name, exception.Message);

        errorViewModel.RequestId = context.TraceIdentifier;

        // Зберегти ErrorViewModel у Items для використання в Error View
        context.Items["ErrorViewModel"] = errorViewModel;

        // Зберегти оригінальний URL
        var originalPath = context.Request.Path;
        var originalQueryString = context.Request.QueryString;

        // Перенаправити на Error action
        context.Request.Path = "/Home/Error";
        context.Request.QueryString = QueryString.Empty;
        context.Request.Method = "GET";

        try
        {
            // Виконати Error action через routing
            await _next(context);
        }
        catch
        {
            // Якщо Error view не може бути отримана, повернути стилізований HTML
            context.Response.ContentType = "text/html; charset=utf-8";

            var html = $@"
<!DOCTYPE html>
<html lang='uk'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{errorViewModel.Title}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
        }}
        .error-container {{
            background: white;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            padding: 60px 40px;
            max-width: 600px;
            text-align: center;
        }}
        .status-code {{
            font-size: 120px;
            font-weight: bold;
            color: #dc3545;
            line-height: 1;
            margin-bottom: 20px;
        }}
        .error-title {{
            font-size: 32px;
            color: #333;
            margin-bottom: 15px;
        }}
        .error-message {{
            font-size: 16px;
            color: #666;
            margin-bottom: 30px;
            line-height: 1.6;
        }}
        .error-actions {{
            display: flex;
            gap: 15px;
            justify-content: center;
            margin-bottom: 30px;
        }}
        .btn {{
            display: inline-block;
            padding: 12px 30px;
            border-radius: 5px;
            text-decoration: none;
            font-weight: 500;
            transition: all 0.3s ease;
            border: none;
            cursor: pointer;
            font-size: 14px;
        }}
        .btn-primary {{
            background: #667eea;
            color: white;
        }}
        .btn-primary:hover {{
            background: #5568d3;
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(102, 126, 234, 0.4);
        }}
        .btn-secondary {{
            background: #f8f9fa;
            color: #667eea;
            border: 2px solid #667eea;
        }}
        .btn-secondary:hover {{
            background: #f0f0f0;
            transform: translateY(-2px);
        }}
        .debug-section {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #eee;
            text-align: left;
            font-size: 12px;
            color: #999;
        }}
        .debug-label {{
            font-weight: 600;
            color: #555;
        }}
    </style>
</head>
<body>
    <div class='error-container'>
        <div class='status-code'>{errorViewModel.StatusCode}</div>
        <h1 class='error-title'>{errorViewModel.Title}</h1>
        <p class='error-message'>{errorViewModel.Message}</p>
        
        <div class='error-actions'>
            <a href='/' class='btn btn-primary'>Повернутись на головну</a>
        </div>
        
        <div class='debug-section'>
            <div><span class='debug-label'>Request ID:</span> {errorViewModel.RequestId}</div>
        </div>
    </div>
</body>
</html>";

            await context.Response.WriteAsync(html);
        }
    }
}
