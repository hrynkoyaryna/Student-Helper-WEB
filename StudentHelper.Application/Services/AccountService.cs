using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using StudentHelper.Application.Models;

namespace StudentHelper.Application.Services;

public class AccountService : IAccountService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountService> _logger;
    private readonly UserManager<User> _userManager;

    public AccountService(IEmailSender emailSender, ILogger<AccountService> logger, UserManager<User> userManager)
    {
        _emailSender = emailSender;
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<Result> SendPasswordResetEmailAsync(User user, string callbackUrl)
    {
        try
        {
            var to = user.Email ?? string.Empty;
            var subject = "Скидання пароля";
            var html = $"Клацніть на посилання для скидання пароля: <a href=\"{callbackUrl}\">тут</a>";

            await _emailSender.SendEmailAsync(to, subject, html);
            _logger.LogInformation("Password reset email sent to {Email}", to);
            return Result.Ok("Посилання для скидання пароля надіслано");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {UserId}", user.Id);
            return Result.Fail("Не вдалося надіслати лист. Спробуйте пізніше.");
        }
    }

    public async Task<Result> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
            return Result.Fail("Поточний пароль не може бути пустим");

        if (string.IsNullOrWhiteSpace(newPassword))
            return Result.Fail("Новий пароль не може бути пустим");

        if (currentPassword == newPassword)
            return Result.Fail("Новий пароль має відрізнятися від поточного");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} changed password successfully", user.Id);
            return Result.Ok("Пароль успішно змінено");
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("Failed to change password for user {UserId}: {Errors}", user.Id, errors);
        return Result.Fail(errors);
    }
}
