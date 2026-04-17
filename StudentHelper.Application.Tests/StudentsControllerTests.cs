using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Controllers;
using StudentHelper.Web.Models.Students;
using Xunit;

namespace StudentHelper.Application.Tests;

public class StudentsControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewResult_WithOnlyNonAdminUsers()
    {
        var users = new List<User>
        {
            new User { Id = 1, UserName = "student1", Email = "student1@test.com" },
            new User { Id = 2, UserName = "admin1", Email = "admin1@test.com" },
        }.AsQueryable();

        var userManagerMock = GetUserManagerMock(users);

        userManagerMock
            .Setup(x => x.IsInRoleAsync(It.Is<User>(u => u.Id == 1), "Admin"))
            .ReturnsAsync(false);

        userManagerMock
            .Setup(x => x.IsInRoleAsync(It.Is<User>(u => u.Id == 2), "Admin"))
            .ReturnsAsync(true);

        var controller = new StudentsController(userManagerMock.Object);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<User>>(viewResult.Model);

        Assert.Single(model);
        Assert.Equal("student1", model.First().UserName);
    }

    [Fact]
    public void Create_Get_ReturnsViewResult()
    {
        var userManagerMock = GetUserManagerMock(new List<User>().AsQueryable());
        var controller = new StudentsController(userManagerMock.Object);

        var result = controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<CreateStudentViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Create_Post_ValidModel_RedirectsToIndex()
    {
        var userManagerMock = GetUserManagerMock(new List<User>().AsQueryable());

        userManagerMock
            .Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        var controller = new StudentsController(userManagerMock.Object);

        var model = new CreateStudentViewModel
        {
            UserName = "student1",
            Email = "student1@test.com",
            Password = "Password123!",
            FirstName = "Ім'я",
            LastName = "Прізвище",
        };

        var result = await controller.Create(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task Edit_Get_UserNotFound_ReturnsNotFound()
    {
        var userManagerMock = GetUserManagerMock(new List<User>().AsQueryable());

        userManagerMock
            .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var controller = new StudentsController(userManagerMock.Object);

        var result = await controller.Edit(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_UserNotFound_ReturnsNotFound()
    {
        var userManagerMock = GetUserManagerMock(new List<User>().AsQueryable());

        userManagerMock
            .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var controller = new StudentsController(userManagerMock.Object);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_AdminUser_ReturnsForbid()
    {
        var user = new User { Id = 1, UserName = "admin1", Email = "admin@test.com" };
        var userManagerMock = GetUserManagerMock(new List<User>().AsQueryable());

        userManagerMock
            .Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(true);

        var controller = new StudentsController(userManagerMock.Object);

        var result = await controller.Delete(1);

        Assert.IsType<ForbidResult>(result);
    }

    private static Mock<UserManager<User>> GetUserManagerMock(IQueryable<User> users)
    {
        var store = new Mock<IUserStore<User>>();

        var mock = new Mock<UserManager<User>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        var userStore = new TestAsyncEnumerable<User>(users);
        mock.Setup(x => x.Users).Returns(userStore);

        return mock;
    }
}

public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    {
    }

    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
        : base(expression)
    {
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        this.inner = inner;
    }

    public T Current => this.inner.Current;

    public ValueTask DisposeAsync()
    {
        this.inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(this.inner.MoveNext());
    }
}

public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider inner;

    public TestAsyncQueryProvider(IQueryProvider inner)
    {
        this.inner = inner;
    }

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(System.Linq.Expressions.Expression expression)
    {
        return this.inner.Execute(expression)!;
    }

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
    {
        return this.inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        return this.Execute<TResult>(expression);
    }
}