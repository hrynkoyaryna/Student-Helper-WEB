using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHelper.Web.Controllers;

/// <summary>
/// Базовий контролер з загальною функціональністю для всіх [Authorize] контролерів.
/// </summary>
[Authorize]
public class BaseController : Controller
{
    /// <summary>
    /// Отримує ID поточного авторизованого користувача з Claims.
    /// </summary>
    /// <returns>ID користувача.</returns>
    /// <exception cref="UnauthorizedAccessException">Якщо користувач не аутентифікований.</exception>
    protected int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Користувач не аутентифікований.");
        }

        return int.Parse(userId);
    }
}
