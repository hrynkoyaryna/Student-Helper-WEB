namespace StudentHelper.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// USE-CASE: Login - аутентифікація користувача за email та паролем
    /// </summary>
    Task<(bool Success, int? UserId, string Message)> LoginAsync(string email, string password);

    /// <summary>
    /// USE-CASE: Register - реєстрація нового користувача
    /// </summary>
    Task<(bool Success, string Message, List<string> Errors)> RegisterAsync(string firstName, string lastName, string email, string password, int? groupId = null);

    /// <summary>
    /// USE-CASE: ForgotPassword - генерування посилання для скидання пароля
    /// </summary>
    Task<(bool Success, string Message, string? ResetLink)> ForgotPasswordAsync(string email, Func<string, string, string> generateResetLink);

    /// <summary>
    /// USE-CASE: ResetPassword - скидання пароля користувачем
    /// </summary>
    Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string encodedToken, string newPassword);
}
