using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Web.Controllers;
using StudentHelper.Web.Models.Students;
using Xunit;

namespace StudentHelper.Application.Tests
{
    public class StudentsControllerGroupTests
    {
        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            var mgr = new Mock<UserManager<User>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            return mgr;
        }

        [Fact]
        public async Task Create_Post_WithNewGroup_CreatesGroupAndRedirects()
        {
            var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new StudentHelperDbContext(options);

            var userManagerMock = CreateUserManagerMock();

            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);
            userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);
            userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new StudentsController(userManagerMock.Object, context, null);

            var model = new CreateStudentViewModel
            {
                UserName = "student1",
                Email = "student1@test.com",
                Password = "Password123!",
                FirstName = "Ім'я",
                LastName = "Прізвище",
                GroupName = "GroupX"
            };

            var result = await controller.Create(model);

            // verify redirect
            var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            // verify group created in db
            var created = context.Groups.FirstOrDefault(g => g.Name == "GroupX");
            Assert.NotNull(created);
        }

        [Fact]
        public async Task Edit_Post_WithNewGroup_CreatesGroupAndAssignsToUser()
        {
            var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new StudentHelperDbContext(options);

            var user = new User { Id = 1, UserName = "student1", Email = "student1@test.com" };

            var userManagerMock = CreateUserManagerMock();

            userManagerMock.Setup(x => x.FindByIdAsync("1"))
                .ReturnsAsync(user);

            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            User? capturedUser = null;
            userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync(IdentityResult.Success);

            var controller = new StudentsController(userManagerMock.Object, context, null);

            var model = new EditStudentViewModel
            {
                Id = 1,
                UserName = "student1",
                Email = "student1@test.com",
                FirstName = "Ім'я",
                LastName = "Прізвище",
                GroupName = "GroupY"
            };

            var result = await controller.Edit(model);

            var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var created = context.Groups.FirstOrDefault(g => g.Name == "GroupY");
            Assert.NotNull(created);

            Assert.NotNull(capturedUser);
            Assert.Equal(created!.Id, capturedUser!.GroupId);
        }
    }
}
