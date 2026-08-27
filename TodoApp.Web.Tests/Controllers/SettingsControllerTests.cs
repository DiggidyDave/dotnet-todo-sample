using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TodoApp.Web.Controllers;
using TodoApp.Web.Models.Entities;
using TodoApp.Web.Models.ViewModels.Settings;
using TodoApp.Web.Services;
using Xunit;

namespace TodoApp.Web.Tests.Controllers;

public class SettingsControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IUserPreferencesService> _preferencesServiceMock;
    private readonly Mock<ILogger<SettingsController>> _loggerMock;
    private readonly SettingsController _controller;
    private readonly ApplicationUser _testUser;

    public SettingsControllerTests()
    {
        _testUser = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "test@example.com",
            Email = "test@example.com",
            Name = "Test User"
        };

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(_testUser.Id);
        _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        _preferencesServiceMock = new Mock<IUserPreferencesService>();
        _loggerMock = new Mock<ILogger<SettingsController>>();

        _controller = new SettingsController(
            _userManagerMock.Object,
            _preferencesServiceMock.Object,
            _loggerMock.Object);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _testUser.Id) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    [Fact]
    public async Task Index_ReturnsViewWithUserPreferences()
    {
        // Arrange
        var preferences = new UserPreferences
        {
            UserId = _testUser.Id,
            FontSize = "large",
            LineSpacing = "relaxed",
            Theme = "dark",
            ReducedMotion = false
        };
        _preferencesServiceMock.Setup(x => x.GetPreferencesAsync(_testUser.Id))
            .ReturnsAsync(preferences);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AccessibilitySettingsViewModel>(viewResult.Model);
        Assert.Equal("large", model.FontSize);
        Assert.Equal("relaxed", model.LineSpacing);
        Assert.Equal("dark", model.Theme);
        Assert.False(model.ReducedMotion);
    }

    [Fact]
    public async Task Index_ReturnsChallenge_WhenUserNotAuthenticated()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((string?)null);

        // Act
        var result = await _controller.Index();

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task UpdateAccessibility_SavesPreferencesAndRedirects()
    {
        // Arrange
        var model = new AccessibilitySettingsViewModel
        {
            FontSize = "extra-large",
            LineSpacing = "compact",
            Theme = "high-contrast-dark",
            ReducedMotion = true
        };
        _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            _controller.HttpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

        // Act
        var result = await _controller.UpdateAccessibility(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        _preferencesServiceMock.Verify(x => x.UpdatePreferencesAsync(
            _testUser.Id, "extra-large", "compact", "high-contrast-dark", true), Times.Once);
    }

    [Fact]
    public async Task UpdateAccessibility_ReturnsView_WhenModelInvalid()
    {
        // Arrange
        var model = new AccessibilitySettingsViewModel();
        _controller.ModelState.AddModelError("FontSize", "Required");

        // Act
        var result = await _controller.UpdateAccessibility(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Same(model, viewResult.Model);
    }

    [Fact]
    public async Task UpdateAccessibility_ReturnsChallenge_WhenUserNotAuthenticated()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((string?)null);
        var model = new AccessibilitySettingsViewModel();

        // Act
        var result = await _controller.UpdateAccessibility(model);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task UpdateAccessibility_SetsTempDataMessage()
    {
        // Arrange
        var model = new AccessibilitySettingsViewModel
        {
            FontSize = "medium",
            LineSpacing = "normal",
            Theme = "default"
        };
        _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            _controller.HttpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

        // Act
        await _controller.UpdateAccessibility(model);

        // Assert
        Assert.Equal("Settings saved successfully.", _controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task UpdatePreferenceAjax_UpdatesPreferenceAndReturnsSuccess()
    {
        // Arrange
        var request = new SettingsController.UpdatePreferenceRequest
        {
            Key = "theme",
            Value = "ocean"
        };

        // Act
        var result = await _controller.UpdatePreferenceAjax(request);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        _preferencesServiceMock.Verify(x => x.UpdatePreferenceAsync(_testUser.Id, "theme", "ocean"), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferenceAjax_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((string?)null);
        var request = new SettingsController.UpdatePreferenceRequest
        {
            Key = "fontsize",
            Value = "large"
        };

        // Act
        var result = await _controller.UpdatePreferenceAjax(request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdatePreferenceAjax_ReturnsBadRequest_WhenKeyInvalid()
    {
        // Arrange
        var request = new SettingsController.UpdatePreferenceRequest
        {
            Key = "invalidkey",
            Value = "value"
        };
        _preferencesServiceMock.Setup(x => x.UpdatePreferenceAsync(_testUser.Id, "invalidkey", "value"))
            .ThrowsAsync(new ArgumentException("Unknown preference key: invalidkey"));

        // Act
        var result = await _controller.UpdatePreferenceAjax(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    }
}
