using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Controllers;
using StudentHelper.Web.Models.Students;
using Xunit;
using System.Linq.Expressions;

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
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var userStore = new TestAsyncEnumerable<User>(users);
        mock.Setup(x => x.Users).Returns(userStore);

        return mock;
    }
}

// --- ДОПОМІЖНІ КЛАСИ (ЯКІ БУЛИ ВИДАЛЕНІ) ---

public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) 
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => this.inner = inner;
    public T Current => inner.Current;
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(inner.MoveNext());
}

public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider inner;
    public TestAsyncQueryProvider(IQueryProvider inner) => this.inner = inner;
    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
    public object Execute(Expression expression) => inner.Execute(expression)!;
    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken token = default) => Execute<TResult>(expression);
}