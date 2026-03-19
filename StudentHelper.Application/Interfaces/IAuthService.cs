namespace StudentHelper.Application.Interfaces;

public interface IAuthService
{
    Task<bool> RegisterAsync(string firstName, string lastName, string email, string password, int? groupId = null);
    Task<(bool Success, int? UserId)> LoginAsync(string email, string password);
}
