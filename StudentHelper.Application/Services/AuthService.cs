using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using System.Text;

namespace StudentHelper.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthService> _logger;
    private readonly IOptions<ApplicationSettings> _settings;

    public AuthService(UserManager<User> userManager, IEmailSender emailSender, ILogger<AuthService> logger, IOptions<ApplicationSettings> settings)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
        _settings = settings;
    }

    // ========== LOGIN USE-CASE ==========
    public async Task<Result<int?>> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result<int?>.Fail("Невірний email або пароль");
        }

        // Check if user is blocked
        if (user.IsBlocked)
        {
            _logger.LogWarning("Blocked user attempted to login: {Email}", email);
            return Result<int?>.Fail("Ваш акаунт заблокований. Будь ласка, зв'яжіться з адміністратором");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return Result<int?>.Fail("Невірний email або пароль");
        }

        return user.Id;
    }

    // ========== REGISTER USE-CASE ==========
    public async Task<Result> RegisterAsync(string firstName, string lastName, string email, string password, int? groupId = null)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return Result.Fail("Користувач з таким email вже зареєстрований");
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
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result.Fail(string.Join(", ", errors));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors.Select(e => e.Description).ToList();
            return Result.Fail("Користувача створено, але не вдалося призначити роль: " + string.Join(", ", errors));
        }

        return true;
    }

    // ========== FORGOT PASSWORD USE-CASE ==========
    public async Task<Result<string?>> ForgotPasswordAsync(string email, Func<string, string, string> generateResetLink)
    {
        var user = await _userManager.FindByEmailAsync(email);
        
        // Регулярне повідомлення для обох випадків - не розкриваємо інформацію про користувача
        const string successMessage = "Посилання для скидання пароля надіслано на вашу email";
        
        if (user == null)
        {
            // Користувача немає, але повертаємо тосамо повідомлення (захист від User Enumeration Attack)
            return Result<string?>.Ok(null, successMessage);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var codeBytes = Encoding.UTF8.GetBytes(token);
        var codeEncoded = WebEncoders.Base64UrlEncode(codeBytes);

        var resetLink = generateResetLink(user.Id.ToString(), codeEncoded);

        await _emailSender.SendEmailAsync(
            user.Email ?? string.Empty,
            "Скидання пароля",
            $"Клацніть на посилання для скидання пароля: <a href=\"{resetLink}\">тут</a>");

        return Result<string?>.Ok(null, successMessage);
    }

    // ========== RESET PASSWORD USE-CASE ==========
    public async Task<Result> ResetPasswordAsync(int userId, string encodedToken, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Fail("Користувача не знайдено");
        }

        // Декодування токена
        var codeBytes = WebEncoders.Base64UrlDecode(encodedToken);
        var decodedToken = Encoding.UTF8.GetString(codeBytes);

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);
        if (result.Succeeded)
        {
            return "Пароль успішно скинуто";
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return Result.Fail(errors);
    }
}
