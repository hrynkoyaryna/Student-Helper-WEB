using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces;

/// <summary>
/// Сервіс для роботи з даними користувача
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Отримати користувача за ID
    /// </summary>
    Task<User?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Отримати користувача за email
    /// </summary>
    Task<User?> GetUserByEmailAsync(string email);
}
