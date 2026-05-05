using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Xunit;
using System.Threading.Tasks;

namespace StudentHelper.Application.Tests;

public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<User>>();
        // Виправлено: передача null! у конструктор для NRT
        _mockUserManager = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _mockEmailSender = new Mock<IEmailSender>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        var settings = Options.Create(new ApplicationSettings
        {
            MinSearchCharacters = 3,
            ItemsPerPage = 10,
            CalendarStartHour = 8,
            MaxTaskDescriptionLength = 500,
            PasswordSettings = new PasswordSettings
            {
                RequiredLength = 8,
                RequireDigit = true,
                RequireNonAlphanumeric = true,
                RequireUppercase = true,
                RequireLowercase = true
            }
        });
        _authService = new AuthService(_mockUserManager.Object, _mockEmailSender.Object, _mockLogger.Object, settings);
    }

    [Fact]
    public async Task LoginAsync_UserExistsWithValidPassword_ReturnsSuccessWithUserId()
    {
        var email = "test@example.com";
        var password = "ValidPassword123!";
        var userId = 1;
        var user = new User { Id = userId, Email = email, UserName = email };

        _mockUserManager.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user);
        _mockUserManager.Setup(um => um.CheckPasswordAsync(user, password)).ReturnsAsync(true);

        var result = await _authService.LoginAsync(email, password);

        Assert.True(result.Success);
        Assert.Equal(userId, result.Value);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFailure()
    {
        var email = "nonexistent@example.com";
        var password = "AnyPassword123!";

        // Виправлено: явне приведення до (User?)null
        _mockUserManager.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync((User?)null);

        var result = await _authService.LoginAsync(email, password);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        var email = "test@example.com";
        var wrongPassword = "WrongPassword123!";
        var user = new User { Id = 1, Email = email, UserName = email };

        _mockUserManager.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user);
        _mockUserManager.Setup(um => um.CheckPasswordAsync(user, wrongPassword)).ReturnsAsync(false);

        var result = await _authService.LoginAsync(email, wrongPassword);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task LoginAsync_EmptyEmail_ReturnsFailure()
    {
        var email = "";
        var password = "ValidPassword123!";

        _mockUserManager.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync((User?)null);

        var result = await _authService.LoginAsync(email, password);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccess()
    {
        var email = "john@example.com";
        var password = "SecurePassword123!";

        _mockUserManager.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync((User?)null);
        _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<User>(), password)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(um => um.AddToRoleAsync(It.IsAny<User>(), "User")).ReturnsAsync(IdentityResult.Success);

        var result = await _authService.RegisterAsync("John", "Doe", email, password);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFail()
    {
        var email = "duplicate@example.com";
        var duplicateUser = new User { Email = email };

        _mockUserManager.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(duplicateUser);

        var result = await _authService.RegisterAsync("John", "Doe", email, "Password123!");

        Assert.False(result.Success);
    }
}