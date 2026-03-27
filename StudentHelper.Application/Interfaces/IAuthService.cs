using StudentHelper.Application.Models;

namespace StudentHelper.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// USE-CASE: Login - аутентифікація користувача за email та паролем.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <param name="password">User password.</param>
    /// <returns>Result with nullable user id when successful.</returns>
    Task<Result<int?>> LoginAsync(string email, string password);

    /// <summary>
    /// USE-CASE: Register - реєстрація нового користувача.
    /// </summary>
    /// <param name="firstName">First name.</param>
    /// <param name="lastName">Last name.</param>
    /// <param name="email">Email.</param>
    /// <param name="password">Password.</param>
    /// <param name="groupId">Optional group id.</param>
    /// <returns>Result containing a list of errors when registration fails; empty list or ok when succeeds.</returns>
    Task<Result<List<string>>> RegisterAsync(string firstName, string lastName, string email, string password, int? groupId = null);

    /// <summary>
    /// USE-CASE: ForgotPassword - генерування посилання для скидання пароля.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <param name="generateResetLink">Function to generate reset link from userId and code.</param>
    /// <returns>Result with reset link (nullable) and message.</returns>
    Task<Result<string?>> ForgotPasswordAsync(string email, Func<string, string, string> generateResetLink);

    /// <summary>
    /// USE-CASE: ResetPassword - скидання пароля користувачем.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="encodedToken">Encoded reset token.</param>
    /// <param name="newPassword">New password.</param>
    /// <returns>Result of reset operation.</returns>
    Task<Result> ResetPasswordAsync(int userId, string encodedToken, string newPassword);
}
