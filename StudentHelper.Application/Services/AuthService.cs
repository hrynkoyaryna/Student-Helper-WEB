using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using System.Text;

namespace StudentHelper.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;

    public AuthService(UserManager<User> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    // ========== LOGIN USE-CASE ==========
    public async Task<(bool Success, int? UserId, string Message)> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return (false, null, "Невірний email або пароль");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return (false, null, "Невірний email або пароль");
        }

        return (true, user.Id, "");
    }

    // ========== REGISTER USE-CASE ==========
    public async Task<(bool Success, string Message, List<string> Errors)> RegisterAsync(string firstName, string lastName, string email, string password, int? groupId = null)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return (false, "", new List<string> { "Користувач з таким email вже зареєстрований" });
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            GroupId = groupId,
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            return (true, "Реєстрація успішна", new List<string>());
        }

        var errors = result.Errors.Select(e => e.Description).ToList();
        return (false, "", errors);
    }

    // ========== FORGOT PASSWORD USE-CASE ==========
    public async Task<(bool Success, string Message, string? ResetLink)> ForgotPasswordAsync(string email, Func<string, string, string> generateResetLink)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Не розкриваємо, що користувача не існує (безпека)
            return (true, "Якщо цей email зареєстрований, ви отримаєте посилання для скидання пароля", null);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var codeBytes = Encoding.UTF8.GetBytes(token);
        var codeEncoded = WebEncoders.Base64UrlEncode(codeBytes);

        var resetLink = generateResetLink(user.Id.ToString(), codeEncoded);

        await _emailSender.SendEmailAsync(
            user.Email ?? string.Empty,
            "Скидання пароля",
            $"Клацніть на посилання для скидання пароля: <a href=\"{resetLink}\">тут</a>");

        return (true, "Посилання для скидання пароля надіслано на вашу email", null);
    }

    // ========== RESET PASSWORD USE-CASE ==========
    public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string encodedToken, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return (false, "Користувача не знайдено");
        }

        // Декодування токена
        var codeBytes = WebEncoders.Base64UrlDecode(encodedToken);
        var decodedToken = Encoding.UTF8.GetString(codeBytes);

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);
        if (result.Succeeded)
        {
            return (true, "Пароль успішно скинуто");
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return (false, errors);
    }
}
