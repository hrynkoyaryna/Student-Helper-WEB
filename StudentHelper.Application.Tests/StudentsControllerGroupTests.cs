using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Web.Controllers;
using StudentHelper.Web.Models.Students;
using Xunit;

namespace StudentHelper.Application.Tests;

public class StudentsControllerGroupTests
{
    [Fact]
    public async Task Create_Post_WithNewGroup_CreatesGroupAndRedirects()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new StudentHelperDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var userManagerMock = CreateUserManagerMock();

        userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        var controller = new StudentsController(
            userManagerMock.Object,
            context,
            NullLogger<StudentsController>.Instance);

        var model = new CreateStudentViewModel
        {
            UserName = "student1",
            Email = "student1@test.com",
            Password = "Password123!",
            FirstName = "Імя",
            LastName = "Прізвище",
            GroupName = "GroupX",
        };

        var result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var created = await context.Groups.FirstOrDefaultAsync(g => g.Name == "GroupX");
        Assert.NotNull(created);
    }

    [Fact]
    public async Task Edit_Post_WithNewGroup_CreatesGroupAndAssignsToUser()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new StudentHelperDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = 1,
            UserName = "student1",
            Email = "student1@test.com",
            FirstName = "Імя",
            LastName = "Прізвище",
        };

        var userManagerMock = CreateUserManagerMock();

        userManagerMock.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(user);

        userManagerMock.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(false);

        userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        User? capturedUser = null;
        userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .ReturnsAsync(IdentityResult.Success);

        var controller = new StudentsController(
            userManagerMock.Object,
            context,
            NullLogger<StudentsController>.Instance);

        var model = new EditStudentViewModel
        {
            Id = 1,
            UserName = "student1",
            Email = "student1@test.com",
            FirstName = "Імя",
            LastName = "Прізвище",
            GroupName = "GroupY",
        };

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var created = await context.Groups.FirstOrDefaultAsync(g => g.Name == "GroupY");
        Assert.NotNull(created);

        Assert.NotNull(capturedUser);
        Assert.Equal(created!.Id, capturedUser!.GroupId);
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}