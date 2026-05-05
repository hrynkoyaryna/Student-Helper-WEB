using StudentHelper.Domain.Entities;
using StudentHelper.Application.Models;

namespace StudentHelper.Application.Interfaces;

public interface IAccountService
{
    Task<Result> SendPasswordResetEmailAsync(User user, string callbackUrl);
    Task<Result> ChangePasswordAsync(User user, string currentPassword, string newPassword);
    
    /// <summary>
    /// USE-CASE: BlockStudent - адміністратор блокує студента, він не може логінитися.
    /// </summary>
    /// <param name="userId">User id to block.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> BlockStudentAsync(int userId);
    
    /// <summary>
    /// USE-CASE: UnblockStudent - адміністратор розблоковує студента.
    /// </summary>
    /// <param name="userId">User id to unblock.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UnblockStudentAsync(int userId);
}
