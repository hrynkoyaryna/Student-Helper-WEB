using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Controllers;
using StudentHelper.Web.Models.Settings;
using Xunit;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using StudentHelper.Application.Models;

namespace StudentHelper.Application.Tests.Controllers;

public class SettingsControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<ILogger<SettingsController>> _loggerMock;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _accountServiceMock = new Mock<IAccountService>();

        var userStoreMock = new Mock<IUserStore<User>>();
        // Виправлено: null! для UserManager
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();

        // Виправлено: null! для SignInManager
        _signInManagerMock = new Mock<SignInManager<User>>(
            userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            null!, null!, null!, null!);

        _loggerMock = new Mock<ILogger<SettingsController>>();

        _controller = new SettingsController(
            _userServiceMock.Object,
            _accountServiceMock.Object,
            _signInManagerMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Index_ThrowsUnauthorizedAccessException_WhenUserNotFound()
    {
        var userId = 1;
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "mock"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        // Виправлено: явне приведення до (User?)null
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.Index());
    }

    [Fact]
    public async Task EditProfile_Post_ReturnsViewResult_WithModelError_WhenUpdateFails()
    {
        var userId = 1;
        var model = new EditProfileViewModel { FirstName = "NewName", LastName = "NewLastName", Email = "new@studenthelper.com" };
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "mock"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
        var errorMessage = "Update failed";

        _userServiceMock.Setup(x => x.UpdateProfileAsync(userId, model.FirstName, model.LastName, model.Email))
            .ReturnsAsync(Result.Fail(errorMessage));

        var result = await _controller.EditProfile(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        // Виправлено: використання оператора ! для ModelState
        Assert.Equal(errorMessage, _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
    }
}