using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Application.Models;
using StudentHelper.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace StudentHelper.Infrastructure.Services; // Змінено на Infrastructure

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly StudentHelperDbContext _context;

    public UserService(UserManager<User> userManager, StudentHelperDbContext context)
    {
        
        _userManager = userManager;
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        // Додаємо .Include(u => u.Group), щоб адмін бачив назву групи в профілі
        return await _userManager.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    // Новий метод для АДМІНА (з групою)
    public async Task<Result> UpdateStudentAsync(int id, string firstName, string lastName, string email, int? groupId)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return Result.Fail("Студента не знайдено.");

        // Перевірка унікальності Email (якщо він змінився)
        if (user.Email != email)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return Result.Fail("Цей Email вже зайнятий іншим користувачем.");
            }
            user.Email = email;
            user.UserName = email;
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.GroupId = groupId; // Оновлюємо групу!

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Ok() : Result.Fail("Помилка при оновленні даних студента.");
    }

    // Отримання всіх груп (для випадаючого списку)
    public async Task<IEnumerable<Group>> GetAllGroupsAsync()
    {
        return await _context.Groups.OrderBy(g => g.Name).ToListAsync();
    }

    public async Task<Result> UpdateProfileAsync(int userId, string firstName, string lastName, string email)
    {
        return await UpdateStudentAsync(userId, firstName, lastName, email, null);
        // null тут означає, що звичайний користувач не міняє свою групу через цей метод
    }

    public async Task<Result> DeleteUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Fail("Користувач не знайдений");

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded ? Result.Ok() : Result.Fail("Помилка при видаленні");
    }
}