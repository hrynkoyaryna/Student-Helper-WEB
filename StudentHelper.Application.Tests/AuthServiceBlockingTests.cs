using System;
using System.Threading.Tasks;
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

/// <summary>
/// Test cases for login with student blocking functionality.
/// These tests verify that:
/// 1. Blocked students cannot login
/// 2. Non-blocked students can login normally
/// 3. Appropriate error messages are shown
/// 4. Logging occurs for blocked login attempts
/// </summary>
public class AuthServiceBlockingTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceBlockingTests()
    {
        var store = new Mock<IUserStore<User>>();
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

    // ========== BLOCKED STUDENT LOGIN TESTS ==========

    [Fact]
    public async Task LoginAsync_BlockedUserAttemptsLogin_ReturnsFailure()
    {
        // Arrange
        var email = "blocked@example.com";
        var password = "ValidPassword123!";
        var userId = 1;
        var blockedUser = new User 
        { 
            Id = userId, 
            Email = email, 
            UserName = email,
            IsBlocked = true 
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(blockedUser);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Ваш акаунт заблокований. Будь ласка, зв'яжіться з адміністратором", result.Message);
        // Password check should NOT be performed
        _mockUserManager.Verify(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Blocked user attempted to login")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_BlockedUserWithValidPassword_ReturnsFailureBeforePasswordCheck()
    {
        // Arrange
        var email = "blocked@example.com";
        var password = "ValidPassword123!";
        var userId = 1;
        var blockedUser = new User 
        { 
            Id = userId, 
            Email = email, 
            UserName = email,
            IsBlocked = true 
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(blockedUser);

        _mockUserManager
            .Setup(um => um.CheckPasswordAsync(blockedUser, password))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert - Block check happens before password check
        Assert.False(result.Success);
        _mockUserManager.Verify(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_UnblockedUserWithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var email = "active@example.com";
        var password = "ValidPassword123!";
        var userId = 1;
        var activeUser = new User 
        { 
            Id = userId, 
            Email = email, 
            UserName = email,
            IsBlocked = false 
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(activeUser);

        _mockUserManager
            .Setup(um => um.CheckPasswordAsync(activeUser, password))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(userId, result.Value);
        _mockUserManager.Verify(um => um.CheckPasswordAsync(activeUser, password), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UnblockedUserWithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var email = "active@example.com";
        var wrongPassword = "WrongPassword123!";
        var userId = 1;
        var activeUser = new User 
        { 
            Id = userId, 
            Email = email, 
            UserName = email,
            IsBlocked = false 
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(activeUser);

        _mockUserManager
            .Setup(um => um.CheckPasswordAsync(activeUser, wrongPassword))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(email, wrongPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Невірний email або пароль", result.Message);
        _mockUserManager.Verify(um => um.CheckPasswordAsync(activeUser, wrongPassword), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_BlockedUserReceivesSpecificBlockingMessage()
    {
        // Arrange
        var email = "blocked@example.com";
        var password = "AnyPassword123!";
        var blockedUser = new User 
        { 
            Id = 1, 
            Email = email, 
            UserName = email,
            IsBlocked = true 
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(blockedUser);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert - Error message should mention blocking specifically
        Assert.False(result.Success);
        Assert.Contains("заблокований", result.Message);
        Assert.Contains("адміністратором", result.Message);
    }

    [Fact]
    public async Task LoginAsync_UserNotFoundReceivesGenericMessage()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "AnyPassword123!";

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert - Error message should NOT mention specific reason
        Assert.False(result.Success);
        Assert.Equal("Невірний email або пароль", result.Message);
    }

    [Fact]
    public async Task LoginAsync_InvalidPasswordReceivesGenericMessage()
    {
        // Arrange
        var email = "active@example.com";
        var wrongPassword = "WrongPassword123!";
        var user = new User 
        { 
            Id = 1, 
            Email = email, 
            UserName = email,
            IsBlocked = false 
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.CheckPasswordAsync(user, wrongPassword))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(email, wrongPassword);

        // Assert - Error message should NOT mention specific reason
        Assert.False(result.Success);
        Assert.Equal("Невірний email або пароль", result.Message);
    }

    // ========== INTEGRATION SCENARIO TESTS ==========

    [Fact]
    public async Task LoginAsync_StudentBlockedAfterPreviousLogin_CannotLoginAnymore()
    {
        // Arrange
        var email = "student@example.com";
        var password = "ValidPassword123!";
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = email, 
            UserName = email,
            IsBlocked = false 
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        // Act - First login (before blocking)
        var firstLoginResult = await _authService.LoginAsync(email, password);

        // Assert - First login successful
        Assert.True(firstLoginResult.Success);
        Assert.Equal(userId, firstLoginResult.Value);

        // Arrange - Admin blocks the student
        user.IsBlocked = true;
        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // Act - Try to login again (after blocking)
        var secondLoginResult = await _authService.LoginAsync(email, password);

        // Assert - Second login fails due to blocking
        Assert.False(secondLoginResult.Success);
        Assert.Contains("заблокований", secondLoginResult.Message);
        // Password check should not be called in second attempt
        // (it was called in first attempt, so we verify it's called exactly once total)
        _mockUserManager.Verify(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_VerifyBlockStatusCheckedBeforePasswordVerification()
    {
        // Arrange
        var email = "blocked@example.com";
        var password = "Password123!";
        var blockedUser = new User 
        { 
            Id = 1, 
            Email = email, 
            UserName = email,
            IsBlocked = true 
        };

        var callSequence = new CallSequence();

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(blockedUser);

        _mockUserManager
            .Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback(() => callSequence.RecordCheckPasswordCall())
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert - Block check happened, password check should not have been called
        Assert.False(result.Success);
        Assert.False(callSequence.CheckPasswordWasCalled, "CheckPassword should not be called for blocked users");
    }

    [Fact]
    public async Task LoginAsync_VerifyLoggingForBlockedLoginAttempt()
    {
        // Arrange
        var email = "blocked@example.com";
        var password = "Password123!";
        var blockedUser = new User 
        { 
            Id = 1, 
            Email = email, 
            UserName = email,
            IsBlocked = true 
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(blockedUser);

        // Act
        await _authService.LoginAsync(email, password);

        // Assert - Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Blocked user") && 
                    v.ToString()!.Contains(email)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_MultipleBlockedLoginAttempts_EachAttemptIsLogged()
    {
        // Arrange
        var email = "blocked@example.com";
        var password = "Password123!";
        var blockedUser = new User 
        { 
            Id = 1, 
            Email = email, 
            UserName = email,
            IsBlocked = true 
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(blockedUser);

        // Act - Multiple login attempts
        await _authService.LoginAsync(email, password);
        await _authService.LoginAsync(email, password);
        await _authService.LoginAsync(email, password);

        // Assert - Each attempt should be logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Blocked user attempted to login")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }
}

/// <summary>
/// Helper class to track call sequence in tests
/// </summary>
internal class CallSequence
{
    public bool CheckPasswordWasCalled { get; private set; }

    public void RecordCheckPasswordCall()
    {
        CheckPasswordWasCalled = true;
    }
}
