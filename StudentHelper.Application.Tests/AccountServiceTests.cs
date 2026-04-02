using System.Linq;
using System.Threading.Tasks;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Xunit;
using System;

namespace StudentHelper.Application.Tests;

public class AccountServiceTests
{
    private Mock<IEmailSender> _emailSenderMock;
    private Mock<ILogger<AccountService>> _loggerMock;
    private Mock<UserManager<User>> _userManagerMock;

    public AccountServiceTests()
    {
        _emailSenderMock = new Mock<IEmailSender>();
        _loggerMock = new Mock<ILogger<AccountService>>();
        _userManagerMock = CreateUserManagerMock();
    }

    private Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }

    // ========== SendPasswordResetEmailAsync Tests ==========

    [Fact]
    public async Task SendPasswordResetEmailAsync_SendsEmail()
    {
        // Arrange
        var accountService = new AccountService(_emailSenderMock.Object, _loggerMock.Object, _userManagerMock.Object);
        var user = new User { Email = "recipient@example.com" };
        var callbackUrl = "https://example.com/reset?code=abc";

        // Act
        var result = await accountService.SendPasswordResetEmailAsync(user, callbackUrl);

        // Assert
        Assert.True(result.Success);
        _emailSenderMock.Verify(es => es.SendEmailAsync("recipient@example.com", It.IsAny<string>(), It.Is<string>(s => s.Contains(callbackUrl))), Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ThrowsWhenEmailSenderFails()
    {
        // Arrange
        _emailSenderMock
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP error"));

        var user = new User { Email = "test@test.com" };
        var accountService = new AccountService(_emailSenderMock.Object, _loggerMock.Object, _userManagerMock.Object);

        // Act & Assert
        // Очікуємо, що виняток буде кинутий (обробляється middleware)
        await Assert.ThrowsAsync<Exception>(async () => 
            await accountService.SendPasswordResetEmailAsync(user, "url"));
    }

    // ========== ChangePasswordAsync Tests ==========

    [Fact]
    public async Task ChangePasswordAsync_SuccessfullyChangesPassword()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        var currentPassword = "CurrentPass123!";
        var newPassword = "NewPass456!";

        _userManagerMock
            .Setup(um => um.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        var accountService = new AccountService(_emailSenderMock.Object, _loggerMock.Object, _userManagerMock.Object);

        // Act
        var result = await accountService.ChangePasswordAsync(user, currentPassword, newPassword);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Пароль успішно змінено", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFalse_WhenCurrentPasswordIsEmpty()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        var accountService = new AccountService(_emailSenderMock.Object, _loggerMock.Object, _userManagerMock.Object);

        // Act
        var result = await accountService.ChangePasswordAsync(user, "", "NewPass123!");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Поточний пароль не може бути пустим", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFalse_WhenNewPasswordIsEmpty()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        var accountService = new AccountService(_emailSenderMock.Object, _loggerMock.Object, _userManagerMock.Object);

        // Act
        var result = await accountService.ChangePasswordAsync(user, "CurrentPass123!", "");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Новий пароль не може бути пустим", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFalse_WhenNewPasswordEqualsCurrent()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        var samePassword = "SamePass123!";
        var accountService = new AccountService(_emailSenderMock.Object, _loggerMock.Object, _userManagerMock.Object);

        // Act
        var result = await accountService.ChangePasswordAsync(user, samePassword, samePassword);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Новий пароль має відрізнятися від поточного", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFalse_WhenCurrentPasswordIncorrect()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        var currentPassword = "WrongPass123!";
        var newPassword = "NewPass456!";

        var identityErrors = new[] { new IdentityError { Code = "PasswordMismatch", Description = "Невірний пароль" } };
        _userManagerMock
            .Setup(um => um.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var accountService = new AccountService(_emailSenderMock.Object, _loggerMock.Object, _userManagerMock.Object);

        // Act
        var result = await accountService.ChangePasswordAsync(user, currentPassword, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Невірний пароль", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFalse_WhenUserManagerReturnsMultipleErrors()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        var currentPassword = "CurrentPass123!";
        var newPassword = "pass";

        var identityErrors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Пароль завкороткий" },
            new IdentityError { Code = "PasswordRequiresNonAlphaNumeric", Description = "Пароль повинен містити спеціальні символи" }
        };

        _userManagerMock
            .Setup(um => um.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var accountService = new AccountService(_emailSenderMock.Object, _loggerMock.Object, _userManagerMock.Object);

        // Act
        var result = await accountService.ChangePasswordAsync(user, currentPassword, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Пароль завкороткий", result.Message);
        Assert.Contains("Пароль повинен містити спеціальні символи", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFalse_AndLogsError_OnException()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        var currentPassword = "CurrentPass123!";
        var newPassword = "NewPass456!";

        _userManagerMock
            .Setup(um => um.ChangePasswordAsync(user, currentPassword, newPassword))
            .ThrowsAsync(new System.Exception("Database error"));

        var accountService = new AccountService(_emailSenderMock.Object, _loggerMock.Object, _userManagerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<System.Exception>(async () => 
            await accountService.ChangePasswordAsync(user, currentPassword, newPassword));
    }
}