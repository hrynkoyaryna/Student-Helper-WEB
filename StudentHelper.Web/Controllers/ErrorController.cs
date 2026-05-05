using Microsoft.AspNetCore.Mvc;

namespace StudentHelper.Web.Controllers;

/// <summary>
/// Контролер для обробки помилок і статусу запитів.
/// Використовується для перенаправлення на сторінки помилок.
/// </summary>
public class ErrorController : Controller
{
    /// <summary>
    /// Сторінка помилки 404 - Сторінка не знайдена
    /// </summary>
    public new IActionResult NotFound()
    {
        Response.StatusCode = 404;
        return View();
    }

    /// <summary>
    /// Сторінка помилки 500 - Внутрішня помилка сервера
    /// </summary>
    public IActionResult ServerError()
    {
        Response.StatusCode = 500;
        return View();
    }

    /// <summary>
    /// Сторінка помилки 429 - Перевищено ліміт запитів
    /// </summary>
    public IActionResult RateLimitExceeded()
    {
        Response.StatusCode = 429;
        return View();
    }
}
