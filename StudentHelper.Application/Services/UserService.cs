using Microsoft.AspNetCore.Identity;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Application.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StudentHelper.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IScheduleRepository _scheduleRepository;

    public UserService(UserManager<User> userManager, IScheduleRepository scheduleRepository)
    {
        _userManager = userManager;
        _scheduleRepository = scheduleRepository;
    }

    public async Task<User?> GetUserByIdAsync(int userId) 
        => await _userManager.FindByIdAsync(userId.ToString());

    public async Task<User?> GetUserByEmailAsync(string email) 
        => await _userManager.FindByEmailAsync(email);

    // Отримання всіх груп через репозиторій
    public async Task<IEnumerable<Group>> GetAllGroupsAsync()
    {
        return await _scheduleRepository.GetAllGroupsAsync();
    }

    // Створення групи через репозиторій (виправляє помилку з _context)
    public async Task<Result> CreateGroupAsync(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return Result.Fail("Назва групи не може бути порожньою.");

        var group = new Group { Name = groupName };
        
        try 
        {
            await _scheduleRepository.CreateGroupAsync(group); 
            return Result.Ok("Групу успішно створено!");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Помилка при створенні групи: {ex.Message}");
        }
    }

    public async Task<Result> UpdateProfileAsync(int userId, string firstName, string lastName, string email)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Fail("Користувач не знайдений");

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.UserName = email;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Ok() : Result.Fail("Помилка при оновленні профілю");
    }

    public async Task<Result> DeleteUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Fail("Користувач не знайдений");

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded ? Result.Ok() : Result.Fail("Помилка при видаленні користувача");
    }
}