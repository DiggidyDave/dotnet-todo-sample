using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TodoApp.Web.Controllers;
using Xunit;

namespace TodoApp.Web.Tests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void Index_WhenUserNotAuthenticated_ReturnsView()
    {
        // Arrange
        var controller = new HomeController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // Not authenticated
            }
        };

        // Act
        var result = controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewName); // Default view
    }

    [Fact]
    public void Index_WhenUserAuthenticated_RedirectsToTaskIndex()
    {
        // Arrange
        var controller = new HomeController();
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        // Act
        var result = controller.Index();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Task", redirectResult.ControllerName);
    }

    [Fact]
    public void Error_ReturnsView()
    {
        // Arrange
        var controller = new HomeController();

        // Act
        var result = controller.Error();

        // Assert
        Assert.IsType<ViewResult>(result);
    }
}
