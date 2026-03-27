using StudentHelper.Domain.Entities;
using StudentHelper.Application.Models;

namespace StudentHelper.Application.Interfaces;

public interface IAccountService
{
    Task<Result> SendPasswordResetEmailAsync(User user, string callbackUrl);
    Task<Result> ChangePasswordAsync(User user, string currentPassword, string newPassword);
}
