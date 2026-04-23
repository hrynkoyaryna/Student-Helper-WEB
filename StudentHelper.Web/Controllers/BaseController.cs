using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace StudentHelper.Web.Controllers;

/// <summary>
/// Базовий контролер з загальною функціональністю для всіх [Authorize] контролерів.
/// </summary>
[Authorize]
public abstract class BaseController : Controller
{
    protected const string SuccessMessageKey = "SuccessMessage";
    protected const string ErrorMessageKey = "ErrorMessage";

    /// <summary>
    /// Отримує ID поточного авторизованого користувача з Claims.
    /// </summary>
    /// <returns>ID користувача.</returns>
    /// <exception cref="UnauthorizedAccessException">Якщо користувач не аутентифікований.</exception>
    protected int GetCurrentUserId()
    {
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Користувач не аутентифікований.");
        }

        return int.Parse(userId);
    }

    protected void SetSuccessMessage(string message)
    {
        this.TempData[SuccessMessageKey] = message;
    }

    protected void SetErrorMessage(string message)
    {
        this.TempData[ErrorMessageKey] = message;
    }

    protected void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            this.ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}