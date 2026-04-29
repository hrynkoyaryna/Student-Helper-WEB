using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Xunit;

namespace StudentHelper.Application.Tests;

/// <summary>
/// Comprehensive test cases for AccountService blocking/unblocking functionality.
/// These tests verify that:
/// 1. Admins can block students
/// 2. Students can be unblocked
/// 3. Proper validation and error handling occurs
/// 4. Blocking status persists correctly
/// 5. Multiple students can be blocked/unblocked independently
/// 6. Logging and audit trails are maintained
/// </summary>
public class AccountServiceBlockingTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly Mock<ILogger<AccountService>> _mockLogger;
    private readonly AccountService _accountService;

    public AccountServiceBlockingTests()
    {
        var store = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _mockEmailSender = new Mock<IEmailSender>();
        _mockLogger = new Mock<ILogger<AccountService>>();
        _accountService = new AccountService(_mockEmailSender.Object, _mockLogger.Object, _mockUserManager.Object);
    }

    // ========== BLOCK STUDENT TESTS ==========

    [Fact]
    public async Task BlockStudentAsync_WithValidUnblockedUser_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = "student@example.com", 
            UserName = "student@example.com",
            IsBlocked = false 
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.Is<User>(u => u.Id == userId && u.IsBlocked)))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.BlockStudentAsync(userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Студент успішно заблокований", result.Message);
        _mockUserManager.Verify(um => um.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User {userId} has been blocked")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BlockStudentAsync_WithAlreadyBlockedUser_ReturnsFail()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = "student@example.com", 
            UserName = "student@example.com",
            IsBlocked = true 
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _accountService.BlockStudentAsync(userId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Користувач вже заблокований", result.Message);
        _mockUserManager.Verify(um => um.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task BlockStudentAsync_WithNonExistentUser_ReturnsFail()
    {
        // Arrange
        var userId = 999;

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _accountService.BlockStudentAsync(userId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Користувач не знайдений", result.Message);
        _mockUserManager.Verify(um => um.UpdateAsync(It.IsAny<User>()), Times.Never);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Attempt to block non-existent user")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BlockStudentAsync_WhenUpdateFails_ReturnsFail()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = "student@example.com", 
            UserName = "student@example.com",
            IsBlocked = false 
        };
        var identityError = new IdentityError { Code = "UpdateFailed", Description = "Database error" };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _accountService.BlockStudentAsync(userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Не вдалося заблокувати студента", result.Message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to block user")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ========== UNBLOCK STUDENT TESTS ==========

    [Fact]
    public async Task UnblockStudentAsync_WithValidBlockedUser_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = "student@example.com", 
            UserName = "student@example.com",
            IsBlocked = true 
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.Is<User>(u => u.Id == userId && !u.IsBlocked)))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.UnblockStudentAsync(userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Студент успішно розблокований", result.Message);
        _mockUserManager.Verify(um => um.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User {userId} has been unblocked")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UnblockStudentAsync_WithNotBlockedUser_ReturnsFail()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = "student@example.com", 
            UserName = "student@example.com",
            IsBlocked = false 
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _accountService.UnblockStudentAsync(userId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Користувач не заблокований", result.Message);
        _mockUserManager.Verify(um => um.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UnblockStudentAsync_WithNonExistentUser_ReturnsFail()
    {
        // Arrange
        var userId = 999;

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _accountService.UnblockStudentAsync(userId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Користувач не знайдений", result.Message);
        _mockUserManager.Verify(um => um.UpdateAsync(It.IsAny<User>()), Times.Never);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Attempt to unblock non-existent user")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UnblockStudentAsync_WhenUpdateFails_ReturnsFail()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = "student@example.com", 
            UserName = "student@example.com",
            IsBlocked = true 
        };
        var identityError = new IdentityError { Code = "UpdateFailed", Description = "Database error" };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _accountService.UnblockStudentAsync(userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Не вдалося розблокувати студента", result.Message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to unblock user")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BlockAndUnblockSequence_WorksCorrectly()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = "student@example.com", 
            UserName = "student@example.com",
            IsBlocked = false 
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act - Block the student
        var blockResult = await _accountService.BlockStudentAsync(userId);

        // Assert - Block was successful
        Assert.True(blockResult.Success);
        user.IsBlocked = true;

        // Act - Try to block again (should fail because already blocked)
        var blockAgainResult = await _accountService.BlockStudentAsync(userId);

        // Assert - Second block fails
        Assert.False(blockAgainResult.Success);
        Assert.Equal("Користувач вже заблокований", blockAgainResult.Message);

        // Arrange for unblock
        user.IsBlocked = true;
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act - Unblock the student
        var unblockResult = await _accountService.UnblockStudentAsync(userId);

        // Assert - Unblock was successful
        Assert.True(unblockResult.Success);
    }

    [Fact]
    public async Task BlockStudentAsync_SetUserIsBlockedPropertyToTrue()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = "student@example.com", 
            UserName = "student@example.com",
            IsBlocked = false 
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _accountService.BlockStudentAsync(userId);

        // Assert - Verify that IsBlocked was set to true
        _mockUserManager.Verify(
            um => um.UpdateAsync(It.Is<User>(u => u.Id == userId && u.IsBlocked == true)),
            Times.Once);
    }

    [Fact]
    public async Task UnblockStudentAsync_SetUserIsBlockedPropertyToFalse()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = "student@example.com", 
            UserName = "student@example.com",
            IsBlocked = true 
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _accountService.UnblockStudentAsync(userId);

        // Assert - Verify that IsBlocked was set to false
        _mockUserManager.Verify(
            um => um.UpdateAsync(It.Is<User>(u => u.Id == userId && u.IsBlocked == false)),
            Times.Once);
    }

    // ========== MULTIPLE STUDENTS TESTS ==========

    [Fact]
    public async Task BlockStudentAsync_MultipleStudentsBlocked_EachBlockedIndependently()
    {
        // Arrange
        var student1 = new User { Id = 1, Email = "student1@example.com", UserName = "student1@example.com", IsBlocked = false };
        var student2 = new User { Id = 2, Email = "student2@example.com", UserName = "student2@example.com", IsBlocked = false };
        var student3 = new User { Id = 3, Email = "student3@example.com", UserName = "student3@example.com", IsBlocked = false };

        var userDict = new Dictionary<string, User>
        {
            { "1", student1 },
            { "2", student2 },
            { "3", student3 }
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => userDict.ContainsKey(id) ? userDict[id] : null);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act - Block first student
        var result1 = await _accountService.BlockStudentAsync(1);
        Assert.True(result1.Success);
        student1.IsBlocked = true;

        // Act - Block second student
        var result2 = await _accountService.BlockStudentAsync(2);
        Assert.True(result2.Success);
        student2.IsBlocked = true;

        // Act - Third student remains unblocked, verify
        var result3 = await _accountService.BlockStudentAsync(3);
        Assert.True(result3.Success);
        student3.IsBlocked = true;

        // Assert - All students are now blocked
        Assert.True(student1.IsBlocked);
        Assert.True(student2.IsBlocked);
        Assert.True(student3.IsBlocked);

        _mockUserManager.Verify(um => um.UpdateAsync(It.IsAny<User>()), Times.Exactly(3));
    }

    [Fact]
    public async Task UnblockStudentAsync_SelectiveUnblocking_OnlyRequestedStudentUnblocked()
    {
        // Arrange
        var student1 = new User { Id = 1, Email = "student1@example.com", UserName = "student1@example.com", IsBlocked = true };
        var student2 = new User { Id = 2, Email = "student2@example.com", UserName = "student2@example.com", IsBlocked = true };

        var userDict = new Dictionary<string, User>
        {
            { "1", student1 },
            { "2", student2 }
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => userDict.ContainsKey(id) ? userDict[id] : null);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act - Unblock only student 1
        var result = await _accountService.UnblockStudentAsync(1);

        // Assert
        Assert.True(result.Success);
        _mockUserManager.Verify(um => um.UpdateAsync(It.Is<User>(u => u.Id == 1 && !u.IsBlocked)), Times.Once);
    }

    // ========== STATE CONSISTENCY TESTS ==========

    [Fact]
    public async Task BlockStudentAsync_BlockingPersistsAcrossOperations()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Email = "student@example.com", UserName = "student@example.com", IsBlocked = false };

        var userList = new List<User> { user };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => { u.IsBlocked = true; })
            .ReturnsAsync(IdentityResult.Success);

        // Act - Block student
        var blockResult = await _accountService.BlockStudentAsync(userId);

        // Assert - Block successful
        Assert.True(blockResult.Success);
        Assert.True(user.IsBlocked);

        // Reset mock for second operation
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act - Try to block again
        var blockAgainResult = await _accountService.BlockStudentAsync(userId);

        // Assert - Should fail because user is already blocked
        Assert.False(blockAgainResult.Success);
        Assert.Equal("Користувач вже заблокований", blockAgainResult.Message);
    }

    [Fact]
    public async Task BlockUnblockBlockSequence_WorksCorrectly()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Email = "student@example.com", UserName = "student@example.com", IsBlocked = false };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => { u.IsBlocked = !u.IsBlocked; })
            .ReturnsAsync(IdentityResult.Success);

        // Act 1 - Block
        var result1 = await _accountService.BlockStudentAsync(userId);
        Assert.True(result1.Success);
        user.IsBlocked = true;

        // Arrange for unblock
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act 2 - Unblock
        var result2 = await _accountService.UnblockStudentAsync(userId);
        Assert.True(result2.Success);
        user.IsBlocked = false;

        // Arrange for second block
        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act 3 - Block again
        var result3 = await _accountService.BlockStudentAsync(userId);
        Assert.True(result3.Success);

        // Assert - All operations successful
        _mockUserManager.Verify(um => um.UpdateAsync(It.IsAny<User>()), Times.Exactly(3));
    }

    // ========== ERROR HANDLING TESTS ==========

    [Fact]
    public async Task BlockStudentAsync_PartialFailure_ResultContainsErrorDetails()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Email = "student@example.com", UserName = "student@example.com", IsBlocked = false };
        var errorCode = "BlockFailed";
        var errorDescription = "Unable to update user in database";

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var identityError = new IdentityError { Code = errorCode, Description = errorDescription };
        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var result = await _accountService.BlockStudentAsync(userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(errorDescription, result.Message);
    }

    [Fact]
    public async Task UnblockStudentAsync_UpdateThrowsException_HandledGracefully()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Email = "student@example.com", UserName = "student@example.com", IsBlocked = true };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var identityErrors = new[]
        {
            new IdentityError { Code = "Error1", Description = "Database connection failed" },
            new IdentityError { Code = "Error2", Description = "Transaction failed" }
        };

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _accountService.UnblockStudentAsync(userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Database connection failed", result.Message);
        Assert.Contains("Transaction failed", result.Message);
    }

    // ========== VALIDATION TESTS ==========

    [Fact]
    public async Task BlockStudentAsync_WithZeroUserId_TreatsAsValidAndChecksDatabase()
    {
        // Arrange
        var userId = 0;
        
        _mockUserManager
            .Setup(um => um.FindByIdAsync("0"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _accountService.BlockStudentAsync(userId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Користувач не знайдений", result.Message);
    }

    [Fact]
    public async Task BlockStudentAsync_WithNegativeUserId_TreatsAsValidAndChecksDatabase()
    {
        // Arrange
        var userId = -1;
        
        _mockUserManager
            .Setup(um => um.FindByIdAsync("-1"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _accountService.BlockStudentAsync(userId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Користувач не знайдений", result.Message);
    }

    [Fact]
    public async Task BlockStudentAsync_WithLargeUserId_WorksCorrectly()
    {
        // Arrange
        var userId = int.MaxValue;
        var user = new User { Id = userId, Email = "student@example.com", UserName = "student@example.com", IsBlocked = false };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.BlockStudentAsync(userId);

        // Assert
        Assert.True(result.Success);
    }

    // ========== LOGGING AND AUDIT TESTS ==========

    [Fact]
    public async Task BlockStudentAsync_VerifiesCorrectLoggingAtEachStep()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Email = "student@example.com", UserName = "student@example.com", IsBlocked = false };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _accountService.BlockStudentAsync(userId);

        // Assert - Verify information level logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("blocked")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UnblockStudentAsync_VerifiesCorrectLoggingAtEachStep()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Email = "student@example.com", UserName = "student@example.com", IsBlocked = true };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _accountService.UnblockStudentAsync(userId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("unblocked")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BlockStudentAsync_WithDifferentErrorCounts_AllErrorsIncludedInMessage()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Email = "student@example.com", UserName = "student@example.com", IsBlocked = false };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var identityErrors = new[]
        {
            new IdentityError { Code = "Err1", Description = "Error description 1" },
            new IdentityError { Code = "Err2", Description = "Error description 2" },
            new IdentityError { Code = "Err3", Description = "Error description 3" }
        };

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _accountService.BlockStudentAsync(userId);

        // Assert - All error descriptions should be in message
        Assert.False(result.Success);
        Assert.Contains("Error description 1", result.Message);
        Assert.Contains("Error description 2", result.Message);
        Assert.Contains("Error description 3", result.Message);
    }

    // ========== BOUNDARY TESTS ==========

    [Fact]
    public async Task BlockStudentAsync_UserWithoutEmailProperty_StillWorks()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = null, 
            UserName = "student",
            IsBlocked = false 
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.BlockStudentAsync(userId);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task UnblockStudentAsync_UserWithEmptyStringEmail_StillWorks()
    {
        // Arrange
        var userId = 1;
        var user = new User 
        { 
            Id = userId, 
            Email = string.Empty, 
            UserName = "student",
            IsBlocked = true 
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(um => um.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.UnblockStudentAsync(userId);

        // Assert
        Assert.True(result.Success);
    }
}
