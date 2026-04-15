using StudentHelper.Domain.Entities;
using StudentHelper.Application.Models;
using System.Threading.Tasks;

namespace StudentHelper.Application.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<Result> UpdateProfileAsync(int userId, string firstName, string lastName, string email);
    Task<Result> DeleteUserAsync(int userId);
}