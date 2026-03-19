using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

public interface IAccountService
{
    Task SendPasswordResetEmailAsync(User user, string callbackUrl);
}
