using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Xunit;

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
        _mockUserManager = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);
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

    #region LoginAsync Tests

    /// <summary>
    /// Test: User exists and password is correct
    /// Expected: Should return (true, userId)
    /// </summary>
    [Fact]
    public async Task LoginAsync_UserExistsWithValidPassword_ReturnsSuccessWithUserId()
    {
        // Arrange
        var email = "test@example.com";
        var password = "ValidPassword123!";
        var userId = 1;
        var user = new User { Id = userId, Email = email, UserName = email };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(userId, result.Value);
        _mockUserManager.Verify(um => um.FindByEmailAsync(email), Times.Once);
        _mockUserManager.Verify(um => um.CheckPasswordAsync(user, password), Times.Once);
    }

    /// <summary>
    /// Test: User does not exist
    /// Expected: Should return (false, null)
    /// </summary>
    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "AnyPassword123!";

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync((User)null);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.False(result.Success);
        _mockUserManager.Verify(um => um.FindByEmailAsync(email), Times.Once);
        _mockUserManager.Verify(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Test: User exists but password is incorrect
    /// Expected: Should return (false, null)
    /// </summary>
    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var email = "test@example.com";
        var wrongPassword = "WrongPassword123!";
        var user = new User { Id = 1, Email = email, UserName = email };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.CheckPasswordAsync(user, wrongPassword))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(email, wrongPassword);

        // Assert
        Assert.False(result.Success);
        _mockUserManager.Verify(um => um.FindByEmailAsync(email), Times.Once);
        _mockUserManager.Verify(um => um.CheckPasswordAsync(user, wrongPassword), Times.Once);
    }

    /// <summary>
    /// Test: Empty email provided
    /// Expected: Should handle gracefully (returns false)
    /// </summary>
    [Fact]
    public async Task LoginAsync_EmptyEmail_ReturnsFailure()
    {
        // Arrange
        var email = "";
        var password = "ValidPassword123!";

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync((User)null);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.False(result.Success);
    }

    /// <summary>
    /// Test: Empty password provided
    /// Expected: Should return false (password check will fail)
    /// </summary>
    [Fact]
    public async Task LoginAsync_EmptyPassword_ReturnsFailure()
    {
        // Arrange
        var email = "test@example.com";
        var password = "";
        var user = new User { Id = 1, Email = email, UserName = email };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.CheckPasswordAsync(user, password))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.False(result.Success);
    }

    #endregion

    #region RegisterAsync Tests

    /// <summary>
    /// Test: Valid user registration
    /// Expected: Should return true and create user successfully
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccess()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var email = "john@example.com";
        var password = "SecurePassword123!";
        var groupId = 5;

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            GroupId = groupId
        };

        var result = IdentityResult.Success;

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(default(User));

        _mockUserManager
            .Setup(um => um.CreateAsync(It.IsAny<User>(), password))
            .ReturnsAsync(result);

        _mockUserManager
            .Setup(um => um.AddToRoleAsync(It.IsAny<User>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var registrationResult = await _authService.RegisterAsync(firstName, lastName, email, password, groupId);

        // Assert
        Assert.True(registrationResult.Success);
        _mockUserManager.Verify(um => um.CreateAsync(It.IsAny<User>(), password), Times.Once);
    }

    /// <summary>
    /// Test: Registration with duplicate email
    /// Expected: Should return false (duplicate email check)
    /// </summary>
    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFail()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var email = "duplicate@example.com";
        var password = "SecurePassword123!";

        var duplicateUser = new User { Email = email };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(It.Is<string>(e => e == email)))
            .ReturnsAsync(duplicateUser);

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(It.Is<string>(e => e != email)))
            .ReturnsAsync(default(User));

        // Act
        var registrationResult = await _authService.RegisterAsync(firstName, lastName, email, password);

        // Assert
        Assert.False(registrationResult.Success);
    }

    /// <summary>
    /// Test: Registration without group (optional parameter)
    /// Expected: Should return true
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithoutGroup_ReturnsSuccess()
    {
        // Arrange
        var firstName = "Jane";
        var lastName = "Smith";
        var email = "jane@example.com";
        var password = "SecurePassword123!";

        var result = IdentityResult.Success;

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(default(User));

        _mockUserManager
            .Setup(um => um.CreateAsync(It.IsAny<User>(), password))
            .ReturnsAsync(result);

        _mockUserManager
            .Setup(um => um.AddToRoleAsync(It.IsAny<User>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var registrationResult = await _authService.RegisterAsync(firstName, lastName, email, password);

        // Assert
        Assert.True(registrationResult.Success);
    }

    #endregion
}
