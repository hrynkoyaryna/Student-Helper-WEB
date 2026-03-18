using Microsoft.AspNetCore.Identity;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;

    public AuthService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> RegisterAsync(string firstName, string lastName, string email, string password, int? groupId = null)
    {
        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            GroupId = groupId,
        };

        var result = await _userManager.CreateAsync(user, password);
        return result.Succeeded;
    }

    public async Task<(bool Success, int? UserId)> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return (false, null);

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
            return (false, null);

        return (true, user.Id);
    }
}
