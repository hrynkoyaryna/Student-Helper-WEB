using Microsoft.AspNetCore.Identity;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Application.Models;
using System.Threading.Tasks;

namespace StudentHelper.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;

    public UserService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<Result> UpdateProfileAsync(int userId, string firstName, string lastName, string email)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        
        if (user == null) 
        {
            return Result.Fail("Користувач не знайдений");
        }

        if (user.Email != email)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return Result.Fail("Цей Email вже використовується іншим користувачем");
            }

            user.Email = email;
            user.UserName = email;
        }

        user.FirstName = firstName;
        user.LastName = lastName;

        var result = await _userManager.UpdateAsync(user);
        
        return result.Succeeded 
            ? Result.Ok() 
            : Result.Fail("Помилка при оновленні профілю");
    }

    public async Task<Result> DeleteUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        
        if (user == null)
        {
            return Result.Fail("Користувач не знайдений");
        }

        var result = await _userManager.DeleteAsync(user);
        
        return result.Succeeded 
            ? Result.Ok() 
            : Result.Fail("Помилка при видаленні профілю");
    }
}