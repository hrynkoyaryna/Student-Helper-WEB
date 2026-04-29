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
        await _emailSender.SendEmailAsync(
            user.Email!,
            "Скидання пароля",
            $"Для скидання пароля перейдіть за посиланням: {callbackUrl}");

        return true;
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
            return "Пароль успішно змінено";
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("Failed to change password for user {UserId}: {Errors}", user.Id, errors);
        return Result.Fail(errors);
    }

    // ========== BLOCK/UNBLOCK STUDENT USE-CASE ==========
    public async Task<Result> BlockStudentAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Attempt to block non-existent user with id {UserId}", userId);
            return Result.Fail("Користувач не знайдений");
        }

        if (user.IsBlocked)
        {
            _logger.LogInformation("User {UserId} is already blocked", userId);
            return Result.Fail("Користувач вже заблокований");
        }

        user.IsBlocked = true;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} has been blocked by admin", userId);
            return "Студент успішно заблокований";
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        _logger.LogError("Failed to block user {UserId}: {Errors}", userId, errors);
        return Result.Fail("Не вдалося заблокувати студента: " + errors);
    }

    public async Task<Result> UnblockStudentAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Attempt to unblock non-existent user with id {UserId}", userId);
            return Result.Fail("Користувач не знайдений");
        }

        if (!user.IsBlocked)
        {
            _logger.LogInformation("User {UserId} is not blocked", userId);
            return Result.Fail("Користувач не заблокований");
        }

        user.IsBlocked = false;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} has been unblocked by admin", userId);
            return "Студент успішно розблокований";
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        _logger.LogError("Failed to unblock user {UserId}: {Errors}", userId, errors);
        return Result.Fail("Не вдалося розблокувати студента: " + errors);
    }
}
