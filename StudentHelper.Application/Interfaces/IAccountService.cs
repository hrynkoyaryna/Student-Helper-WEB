using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface IAccountService
{
    Task SendPasswordResetEmailAsync(User user, string callbackUrl);
    Task<(bool Success, string Message)> ChangePasswordAsync(User user, string currentPassword, string newPassword);
}
