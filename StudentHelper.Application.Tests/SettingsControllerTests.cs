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
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);
            
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();
        
        _signInManagerMock = new Mock<SignInManager<User>>(
            userManagerMock.Object, 
            contextAccessorMock.Object, 
            claimsFactoryMock.Object, 
            null, null, null, null);

        _loggerMock = new Mock<ILogger<SettingsController>>();

        _controller = new SettingsController(
            _userServiceMock.Object,
            _accountServiceMock.Object,
            _signInManagerMock.Object,
            _loggerMock.Object
        );
    }

    private void SetupUserClaims(string userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithValidUserModel()
    {
        var userId = 1;
        var expectedUser = new User 
        { 
            Id = userId, 
            FirstName = "Kateryna", 
            LastName = "Vitik", 
            Email = "test@studenthelper.com" 
        };

        SetupUserClaims(userId.ToString());
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(expectedUser);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<SettingsIndexViewModel>(viewResult.Model);
        
        Assert.Equal(expectedUser.FirstName, model.FirstName);
        Assert.Equal(expectedUser.LastName, model.LastName);
        Assert.Equal(expectedUser.Email, model.Email);
    }

    [Fact]
    public async Task Index_ThrowsUnauthorizedAccessException_WhenUserNotFound()
    {
        var userId = 1;
        SetupUserClaims(userId.ToString());
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((User)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.Index());
    }
    [Fact]
    public async Task EditProfile_Get_ReturnsViewResult_WithValidModel()
    {
        var userId = 1;
        var expectedUser = new User 
        { 
            Id = userId, 
            FirstName = "Kateryna", 
            LastName = "Vitik", 
            Email = "test@studenthelper.com" 
        };
        SetupUserClaims(userId.ToString());
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(expectedUser);

        var result = await _controller.EditProfile();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<EditProfileViewModel>(viewResult.Model);
        Assert.Equal(expectedUser.FirstName, model.FirstName);
        Assert.Equal(expectedUser.LastName, model.LastName);
        Assert.Equal(expectedUser.Email, model.Email);
    }

    [Fact]
    public async Task EditProfile_Post_ReturnsViewResult_WhenModelStateIsInvalid()
    {
        var model = new EditProfileViewModel();
        _controller.ModelState.AddModelError("FirstName", "Required");

        var result = await _controller.EditProfile(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task EditProfile_Post_RedirectsToIndex_WhenUpdateIsSuccessful()
    {
        var userId = 1;
        var model = new EditProfileViewModel
        {
            FirstName = "NewName",
            LastName = "NewLastName",
            Email = "new@studenthelper.com"
        };
        SetupUserClaims(userId.ToString());
        
        _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        _userServiceMock.Setup(x => x.UpdateProfileAsync(userId, model.FirstName, model.LastName, model.Email))
            .ReturnsAsync(Result.Ok());

        var result = await _controller.EditProfile(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task EditProfile_Post_ReturnsViewResult_WithModelError_WhenUpdateFails()
    {
        var userId = 1;
        var model = new EditProfileViewModel
        {
            FirstName = "NewName",
            LastName = "NewLastName",
            Email = "new@studenthelper.com"
        };
        SetupUserClaims(userId.ToString());
        var errorMessage = "Update failed";

        _userServiceMock.Setup(x => x.UpdateProfileAsync(userId, model.FirstName, model.LastName, model.Email))
            .ReturnsAsync(Result.Fail(errorMessage));

        var result = await _controller.EditProfile(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Equal(errorMessage, _controller.ModelState[string.Empty].Errors[0].ErrorMessage);
    }
    [Fact]
    public async Task DeleteProfile_Post_RedirectsToHome_WhenDeletionIsSuccessful()
    {
        var userId = 1;
        SetupUserClaims(userId.ToString());
        
        _userServiceMock.Setup(x => x.DeleteUserAsync(userId))
            .ReturnsAsync(Result.Ok());
            
        _signInManagerMock.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteProfile();

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteProfile_Post_RedirectsToIndex_WithError_WhenDeletionFails()
    {
        var userId = 1;
        var errorMessage = "Помилка при видаленні";
        SetupUserClaims(userId.ToString());
        
        _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        _userServiceMock.Setup(x => x.DeleteUserAsync(userId))
            .ReturnsAsync(Result.Fail(errorMessage));

        var result = await _controller.DeleteProfile();

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Null(redirectResult.ControllerName);
        Assert.Equal(errorMessage, _controller.TempData["ErrorMessage"]);
    }
}