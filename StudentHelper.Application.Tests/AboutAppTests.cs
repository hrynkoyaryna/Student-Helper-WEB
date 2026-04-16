using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using StudentHelper.Web.Controllers;
using Xunit;

namespace StudentHelper.Tests.Controllers
{
    public class HomeControllerTests
    {
        [Fact]
        public void AboutApp_ReturnsViewResult()
        {
            // Arrange
            var controller = new HomeController();

            // Act
            var result = controller.AboutApp();

            // Assert
            Assert.IsType<ViewResult>(result);
        }
    }
}