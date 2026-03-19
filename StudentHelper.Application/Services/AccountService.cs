using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace StudentHelper.Application.Services;

public class AccountService : IAccountService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IEmailSender emailSender, ILogger<AccountService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(User user, string callbackUrl)
    {
        var to = user.Email ?? string.Empty;
        var subject = "Скидання пароля";
        var html = $"Клацніть на посилання для скидання пароля: <a href=\"{callbackUrl}\">тут</a>";

        try
        {
            await _emailSender.SendEmailAsync(to, subject, html);
            _logger.LogInformation("Password reset email sent to {Email}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", to);
            throw;
        }
    }
}
