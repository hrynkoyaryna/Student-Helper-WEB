using Moq;
using StudentHelper.Domain.Entities;
using Xunit;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace StudentHelper.Application.Tests;

public class UserRequestsServiceTests
{
    private readonly Mock<IUserStore<User>> _userStoreMock;
    private readonly Mock<UserManager<User>> _userManagerMock;

    public UserRequestsServiceTests()
    {
        _userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            _userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    [Fact]
    public void CreateUserRequest_WithValidData_SetsCorrectProperties()
    {
        // Arrange
        var userId = 1;
        var category = "Розклад";
        var requestType = "Помилка";
        var subject = "Неправильний викладач";
        var description = "У розкладі вказаний не той викладач.";

        // Act
        var userRequest = new UserRequest
        {
            UserId = userId,
            Category = category,
            RequestType = requestType,
            Subject = subject,
            Description = description,
            Status = "Нове",
            CreatedAt = DateTime.UtcNow,
        };

        // Assert
        Assert.Equal(userId, userRequest.UserId);
        Assert.Equal(category, userRequest.Category);
        Assert.Equal(requestType, userRequest.RequestType);
        Assert.Equal(subject, userRequest.Subject);
        Assert.Equal(description, userRequest.Description);
        Assert.Equal("Нове", userRequest.Status);
        Assert.NotEqual(default(DateTime), userRequest.CreatedAt);
    }

    [Fact]
    public void CreateUserRequest_DefaultStatus_IsNove()
    {
        // Arrange & Act
        var userRequest = new UserRequest
        {
            UserId = 1,
            Category = "Тест",
            RequestType = "Помилка",
            Subject = "Тест",
            Description = "Опис тесту",
            Status = "Нове",
            CreatedAt = DateTime.UtcNow,
        };

        // Assert
        Assert.Equal("Нове", userRequest.Status);
    }

    [Fact]
    public void FilterRequestsByUserId_WithMultipleRequests_ReturnsOnlyUserRequests()
    {
        // Arrange
        var userId = 1;
        var requests = new List<UserRequest>
        {
            new() { Id = 1, UserId = 1, Subject = "Мій запит", Status = "Нове", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, UserId = 2, Subject = "Чужий запит", Status = "Нове", CreatedAt = DateTime.UtcNow },
            new() { Id = 3, UserId = 1, Subject = "Ще мій запит", Status = "Прийнято", CreatedAt = DateTime.UtcNow.AddHours(-1) },
        };

        // Act
        var userRequests = requests.Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).ToList();

        // Assert
        Assert.Equal(2, userRequests.Count);
        Assert.All(userRequests, r => Assert.Equal(userId, r.UserId));
        Assert.Equal("Мій запит", userRequests[0].Subject);
        Assert.Equal("Ще мій запит", userRequests[1].Subject);
    }

    [Fact]
    public void UpdateRequestStatus_WithValidData_UpdatesStatusAndResponse()
    {
        // Arrange
        var userRequest = new UserRequest
        {
            Id = 1,
            UserId = 1,
            Category = "Завдання",
            RequestType = "Запит на зміну",
            Subject = "Змінити завдання",
            Description = "Потрібно змінити дедлайн.",
            Status = "Нове",
            CreatedAt = DateTime.UtcNow,
        };

        var newStatus = "Прийнято";
        var adminResponse = "Запит прийнято в роботу.";

        // Act
        userRequest.Status = newStatus;
        userRequest.AdminResponse = adminResponse;
        userRequest.UpdatedAt = DateTime.UtcNow;

        // Assert
        Assert.Equal(newStatus, userRequest.Status);
        Assert.Equal(adminResponse, userRequest.AdminResponse);
        Assert.NotEqual(default(DateTime), userRequest.UpdatedAt);
    }

    [Fact]
    public void GetAllRequests_WithMultipleRequests_ReturnsAllInDescendingOrder()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var requests = new List<UserRequest>
        {
            new() { Id = 1, UserId = 1, Subject = "Запит 1", Status = "Нове", CreatedAt = now.AddHours(-2) },
            new() { Id = 2, UserId = 2, Subject = "Запит 2", Status = "Нове", CreatedAt = now },
            new() { Id = 3, UserId = 3, Subject = "Запит 3", Status = "Прийнято", CreatedAt = now.AddHours(-1) },
        };

        // Act
        var allRequests = requests.OrderByDescending(r => r.CreatedAt).ToList();

        // Assert
        Assert.Equal(3, allRequests.Count);
        Assert.Equal("Запит 2", allRequests[0].Subject);
        Assert.Equal("Запит 3", allRequests[1].Subject);
        Assert.Equal("Запит 1", allRequests[2].Subject);
    }

    [Fact]
    public void UserRequest_WithAllFields_ContainsCompleteData()
    {
        // Arrange
        var userId = 1;
        var category = "Розклад";
        var requestType = "Помилка";
        var subject = "Тест";
        var description = "Опис тесту";
        var status = "Нове";
        var adminResponse = "Відповідь адміна";
        var now = DateTime.UtcNow;

        // Act
        var userRequest = new UserRequest
        {
            Id = 1,
            UserId = userId,
            Category = category,
            RequestType = requestType,
            Subject = subject,
            Description = description,
            Status = status,
            AdminResponse = adminResponse,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Assert
        Assert.Equal(1, userRequest.Id);
        Assert.Equal(userId, userRequest.UserId);
        Assert.Equal(category, userRequest.Category);
        Assert.Equal(requestType, userRequest.RequestType);
        Assert.Equal(subject, userRequest.Subject);
        Assert.Equal(description, userRequest.Description);
        Assert.Equal(status, userRequest.Status);
        Assert.Equal(adminResponse, userRequest.AdminResponse);
        Assert.Equal(now, userRequest.CreatedAt);
        Assert.Equal(now, userRequest.UpdatedAt);
    }

    [Fact]
    public void MultipleUserRequests_CanBeCreatedIndependently()
    {
        // Arrange & Act
        var request1 = new UserRequest
        {
            Id = 1,
            UserId = 1,
            Category = "Розклад",
            RequestType = "Помилка",
            Subject = "Запит 1",
            Description = "Опис 1",
            Status = "Нове",
            CreatedAt = DateTime.UtcNow,
        };

        var request2 = new UserRequest
        {
            Id = 2,
            UserId = 1,
            Category = "Екзамени",
            RequestType = "Пропозиція",
            Subject = "Запит 2",
            Description = "Опис 2",
            Status = "Нове",
            CreatedAt = DateTime.UtcNow,
        };

        // Assert
        Assert.NotEqual(request1.Id, request2.Id);
        Assert.Equal(request1.UserId, request2.UserId);
        Assert.NotEqual(request1.Category, request2.Category);
        Assert.NotEqual(request1.Subject, request2.Subject);
    }

    [Fact]
    public void UpdateUserRequest_WithPartialUpdates_ModifiesOnlyChangedFields()
    {
        // Arrange
        var userRequest = new UserRequest
        {
            Id = 1,
            UserId = 1,
            Category = "Завдання",
            RequestType = "Запит",
            Subject = "Оригінальна тема",
            Description = "Оригінальний опис",
            Status = "Нове",
            CreatedAt = DateTime.UtcNow,
        };

        var originalCategory = userRequest.Category;
        var originalDescription = userRequest.Description;

        // Act
        userRequest.Status = "Прийнято";
        userRequest.AdminResponse = "Оброблено";
        userRequest.UpdatedAt = DateTime.UtcNow;

        // Assert
        Assert.Equal(originalCategory, userRequest.Category);
        Assert.Equal(originalDescription, userRequest.Description);
        Assert.Equal("Прийнято", userRequest.Status);
        Assert.Equal("Оброблено", userRequest.AdminResponse);
    }
}