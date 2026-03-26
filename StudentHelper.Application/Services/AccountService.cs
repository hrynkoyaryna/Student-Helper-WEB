using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

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

    public async Task SendPasswordResetEmailAsync(User user, string callbackUrl)
    {
        var to = user.Email ?? string.Empty;
        var subject = "Скидання пароля";
        var html = $"Клацніть на посилання для скидання пароля: <a href=\"{callbackUrl}\">тут</a>";

        await _emailSender.SendEmailAsync(to, subject, html);
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
            return (false, "Поточний пароль не може бути пустим");

        if (string.IsNullOrWhiteSpace(newPassword))
            return (false, "Новий пароль не може бути пустим");

        if (currentPassword == newPassword)
            return (false, "Новий пароль має відрізнятися від поточного");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (result.Succeeded)
        {
            return (true, "Пароль успішно змінено");
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return (false, errors);
    }
}
