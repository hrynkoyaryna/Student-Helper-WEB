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
        await _emailSender.SendEmailAsync(
            user.Email!,
            "Скидання пароля",
            $"Для скидання пароля перейдіть за посиланням: {callbackUrl}");

        return Result.Ok();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Помилка при відправці листа для скидання пароля для користувача {Email}", user.Email);
        
        return Result.Fail("Не вдалося відправити лист для скидання пароля.");
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
