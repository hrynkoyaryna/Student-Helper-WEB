using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Application.Models;
using StudentHelper.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace StudentHelper.Infrastructure.Services;

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
        return await _userManager.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<Result> UpdateStudentAsync(int id, string firstName, string lastName, string email, int? groupId)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return Result.Fail("Студента не знайдено.");

        if (user.Email != email)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != user.Id)
                return Result.Fail("Цей Email вже зайнятий іншим користувачем.");
            user.Email = email;
            user.UserName = email;
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.GroupId = groupId;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Ok() : Result.Fail("Помилка при оновленні даних студента.");
    }

    public async Task<IEnumerable<Group>> GetAllGroupsAsync()
    {
        return await _context.Groups.OrderBy(g => g.Name).ToListAsync();
    }

    public async Task<Result> UpdateProfileAsync(int userId, string firstName, string lastName, string email)
    {
        return await UpdateStudentAsync(userId, firstName, lastName, email, null);
    }

    public async Task<Result> DeleteUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Result.Fail("Користувач не знайдений");

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded ? Result.Ok() : Result.Fail("Помилка при видаленні");
    }

    public async Task<Result> CreateGroupAsync(string groupName)
    {
        var exists = await _context.Groups.AnyAsync(g => g.Name == groupName);
        if (exists) return Result.Fail("Група з такою назвою вже існує.");

        var group = new Group { Name = groupName };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        return Result.Ok();
    }
}
