using System.Linq;
using System.Threading.Tasks;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Microsoft.Extensions.Logging;
using Xunit;

namespace StudentHelper.Application.Tests;

public class AccountServiceTests
{
    [Fact]
    public async Task SendPasswordResetEmailAsync_SendsEmail()
    {
        // Arrange
        var emailSenderMock = new Mock<IEmailSender>();
        var loggerMock = new Mock<ILogger<AccountService>>();

        var accountService = new AccountService(emailSenderMock.Object, loggerMock.Object);

        var user = new User { Email = "recipient@example.com" };
        var callbackUrl = "https://example.com/reset?code=abc";

        // Act
        await accountService.SendPasswordResetEmailAsync(user, callbackUrl);

        // Assert
        emailSenderMock.Verify(es => es.SendEmailAsync("recipient@example.com", It.IsAny<string>(), It.Is<string>(s => s.Contains(callbackUrl))), Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_LogsAndThrows_WhenEmailSenderFails()
    {
        // Arrange
        var emailSenderMock = new Mock<IEmailSender>();
        var loggerMock = new Mock<ILogger<AccountService>>();

        emailSenderMock
            .Setup(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new System.Exception("SMTP error"));

        var accountService = new AccountService(emailSenderMock.Object, loggerMock.Object);

        var user = new User { Email = "recipient@example.com" };

        // Act & Assert
        await Assert.ThrowsAsync<System.Exception>(async () => await accountService.SendPasswordResetEmailAsync(user, "url"));

        // Verify that logger.Log was called with LogLevel.Error and message containing 'Failed to send'
        var wasLogged = loggerMock.Invocations.Any(inv =>
        {
            if (inv.Method.Name != "Log") return false;
            if (inv.Arguments.Count < 3) return false;
            if (!(inv.Arguments[0] is LogLevel level) || level != LogLevel.Error) return false;
            var state = inv.Arguments[2];
            return state?.ToString()?.Contains("Failed to send password reset email") == true;
        });

        Assert.True(wasLogged, "Expected an error log containing 'Failed to send password reset email' but none was found.");
    }
}
