using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly StudentHelperDbContext _context;

    public AuthService(StudentHelperDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RegisterAsync(string firstName, string lastName, string email, string password, int groupId)
    {
        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        if (exists) return false;

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = 2,
            GroupId = groupId,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return true;
    }
}
