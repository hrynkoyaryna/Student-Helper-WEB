using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Web.Controllers;
using StudentHelper.Web.Models.UserRequests;
using Xunit;

namespace StudentHelper.Application.Tests;

public class UserRequestsControllerTests
{
    [Fact]
    public async Task Create_WithValidModel_CreatesUserRequestWithNewStatus()
    {
        // Arrange
        await using var context = CreateContext();

        var user = new User
        {
            Id = 1,
            UserName = "student",
            Email = "student@test.com",
        };

        var userManager = CreateUserManager(user);
        var controller = CreateController(context, userManager.Object, user);

        var model = new CreateUserRequestViewModel
        {
            Category = "Розклад",
            RequestType = "Помилка",
            Subject = "Неправильний викладач",
            Description = "У розкладі вказаний не той викладач.",
        };

        // Act
        var result = await controller.Create(model);

        // Assert
        var request = await context.UserRequests.FirstOrDefaultAsync();

        Assert.NotNull(request);
        Assert.Equal(user.Id, request.UserId);
        Assert.Equal("Розклад", request.Category);
        Assert.Equal("Помилка", request.RequestType);
        Assert.Equal("Неправильний викладач", request.Subject);
        Assert.Equal("Нове", request.Status);
        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsOnlyCurrentUserRequests()
    {
        // Arrange
        await using var context = CreateContext();

        var currentUser = new User
        {
            Id = 1,
            UserName = "student",
            Email = "student@test.com",
        };

        context.UserRequests.AddRange(
            new UserRequest
            {
                UserId = 1,
                Category = "Розклад",
                RequestType = "Помилка",
                Subject = "Мій запит",
                Description = "Опис мого запиту",
                Status = "Нове",
                CreatedAt = DateTime.UtcNow,
            },
            new UserRequest
            {
                UserId = 2,
                Category = "Екзамени",
                RequestType = "Пропозиція",
                Subject = "Чужий запит",
                Description = "Опис чужого запиту",
                Status = "Нове",
                CreatedAt = DateTime.UtcNow,
            });

        await context.SaveChangesAsync();

        var userManager = CreateUserManager(currentUser);
        var controller = CreateController(context, userManager.Object, currentUser);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<UserRequest>>(viewResult.Model);

        Assert.Single(model);
        Assert.Equal("Мій запит", model[0].Subject);
    }

    [Fact]
    public async Task AdminIndex_ReturnsAllUserRequests()
    {
        // Arrange
        await using var context = CreateContext();

        var firstUser = new User
        {
            Id = 1,
            UserName = "student1",
            Email = "student1@test.com",
        };

        var secondUser = new User
        {
            Id = 2,
            UserName = "student2",
            Email = "student2@test.com",
        };

        context.Users.AddRange(firstUser, secondUser);

        context.UserRequests.AddRange(
            new UserRequest
            {
                UserId = firstUser.Id,
                User = firstUser,
                Category = "Розклад",
                RequestType = "Помилка",
                Subject = "Перший запит",
                Description = "Опис першого запиту",
                Status = "Нове",
                CreatedAt = DateTime.UtcNow,
            },
            new UserRequest
            {
                UserId = secondUser.Id,
                User = secondUser,
                Category = "Нотатки",
                RequestType = "Пропозиція",
                Subject = "Другий запит",
                Description = "Опис другого запиту",
                Status = "Нове",
                CreatedAt = DateTime.UtcNow,
            });

        await context.SaveChangesAsync();

        var admin = new User
        {
            Id = 10,
            UserName = "admin",
            Email = "admin@test.com",
        };

        var userManager = CreateUserManager(admin);
        var controller = CreateController(context, userManager.Object, admin, "Admin");

        // Act
        var result = await controller.AdminIndex();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<UserRequest>>(viewResult.Model);

        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task EditStatus_WithValidModel_UpdatesStatusAndAdminResponse()
    {
        // Arrange
        await using var context = CreateContext();

        var request = new UserRequest
        {
            UserId = 1,
            Category = "Завдання",
            RequestType = "Запит на зміну",
            Subject = "Змінити завдання",
            Description = "Потрібно змінити дедлайн.",
            Status = "Нове",
            CreatedAt = DateTime.UtcNow,
        };

        context.UserRequests.Add(request);
        await context.SaveChangesAsync();

        var admin = new User
        {
            Id = 10,
            UserName = "admin",
            Email = "admin@test.com",
        };

        var userManager = CreateUserManager(admin);
        var controller = CreateController(context, userManager.Object, admin, "Admin");

        var model = new UpdateUserRequestStatusViewModel
        {
            Id = request.Id,
            Status = "Прийнято",
            AdminResponse = "Запит прийнято в роботу.",
        };

        // Act
        var result = await controller.EditStatus(model);

        // Assert
        var updatedRequest = await context.UserRequests.FindAsync(request.Id);

        Assert.NotNull(updatedRequest);
        Assert.Equal("Прийнято", updatedRequest.Status);
        Assert.Equal("Запит прийнято в роботу.", updatedRequest.AdminResponse);
        Assert.NotNull(updatedRequest.UpdatedAt);
        Assert.IsType<RedirectToActionResult>(result);
    }

    private static StudentHelperDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StudentHelperDbContext(options);
    }

    private static Mock<UserManager<User>> CreateUserManager(User user)
    {
        var store = new Mock<IUserStore<User>>();

        var userManager = new Mock<UserManager<User>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        userManager
            .Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        return userManager;
    }

    private static UserRequestsController CreateController(
        StudentHelperDbContext context,
        UserManager<User> userManager,
        User user,
        string role = "User")
    {
        var controller = new UserRequestsController(context, userManager);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Role, role),
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal,
            },
        };

        return controller;
    }
}