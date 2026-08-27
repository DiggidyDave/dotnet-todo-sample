using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TodoApp.Web.Controllers;
using TodoApp.Web.Models.Entities;
using TodoApp.Web.Models.ViewModels.Account;
using TodoApp.Web.Services;
using Xunit;

namespace TodoApp.Web.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<AccountController>> _loggerMock;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Setup SignInManager mock
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            userPrincipalFactoryMock.Object,
            null!, null!, null!, null!);

        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<AccountController>>();

        _controller = new AccountController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);

        // Setup controller context
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
    }

    #region Login Tests

    [Fact]
    public void Login_Get_WhenNotAuthenticated_ReturnsView()
    {
        // Act
        var result = _controller.Login(null);

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Login_Get_WhenAuthenticated_RedirectsToTaskIndex()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _controller.Login(null);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Task", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Login_Post_WithValidCredentials_RedirectsToTaskIndex()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "Password123!",
            RememberMe = false
        };

        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.Login(model, null);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Task", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Login_Post_WithInvalidCredentials_ReturnsViewWithError()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.Login(model, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_Post_WhenLockedOut_ReturnsViewWithLockoutMessage()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var result = await _controller.Login(model, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_Post_WithInvalidModel_ReturnsView()
    {
        // Arrange
        var model = new LoginViewModel();
        _controller.ModelState.AddModelError("Email", "Required");

        // Act
        var result = await _controller.Login(model, null);

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    #endregion

    #region Register Tests

    [Fact]
    public void Register_Get_WhenNotAuthenticated_ReturnsView()
    {
        // Act
        var result = _controller.Register();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Register_Get_WhenAuthenticated_RedirectsToTaskIndex()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _controller.Register();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Task", redirectResult.ControllerName);
    }

    [Fact]
    public async Task Register_Post_WithValidModel_CreatesUserAndRedirects()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
            .ReturnsAsync(IdentityResult.Success);

        _signInManagerMock.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Register(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Task", redirectResult.ControllerName);

        _userManagerMock.Verify(x => x.CreateAsync(
            It.Is<ApplicationUser>(u => u.Email == model.Email && u.Name == model.Name),
            model.Password), Times.Once);
    }

    [Fact]
    public async Task Register_Post_WithDuplicateEmail_ReturnsViewWithError()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Name = "Test User",
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email already exists" }));

        // Act
        var result = await _controller.Register(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Register_Post_WithInvalidModel_ReturnsView()
    {
        // Arrange
        var model = new RegisterViewModel();
        _controller.ModelState.AddModelError("Email", "Required");

        // Act
        var result = await _controller.Register(model);

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_SignsOutAndRedirectsToHome()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);

        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    #endregion

    #region ForgotPassword Tests

    [Fact]
    public void ForgotPassword_Get_ReturnsView()
    {
        // Act
        var result = _controller.ForgotPassword();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task ForgotPassword_Post_WithValidEmail_RedirectsToConfirmation()
    {
        // Arrange
        var model = new ForgotPasswordViewModel { Email = "test@example.com" };
        var user = new ApplicationUser { Id = "user-id", Email = model.Email };

        _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token");

        // Setup URL helper mock
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns("http://localhost/reset");
        _controller.Url = urlHelperMock.Object;

        // Act
        var result = await _controller.ForgotPassword(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ForgotPasswordConfirmation", redirectResult.ActionName);
    }

    [Fact]
    public async Task ForgotPassword_Post_WithNonExistentEmail_StillRedirectsToConfirmation()
    {
        // Arrange (to prevent email enumeration)
        var model = new ForgotPasswordViewModel { Email = "nonexistent@example.com" };

        _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.ForgotPassword(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ForgotPasswordConfirmation", redirectResult.ActionName);
    }

    [Fact]
    public async Task ForgotPassword_Post_WithInvalidModel_ReturnsView()
    {
        // Arrange
        var model = new ForgotPasswordViewModel();
        _controller.ModelState.AddModelError("Email", "Required");

        // Act
        var result = await _controller.ForgotPassword(model);

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void ForgotPasswordConfirmation_ReturnsView()
    {
        // Act
        var result = _controller.ForgotPasswordConfirmation();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public void ResetPassword_Get_WithValidParams_ReturnsView()
    {
        // Act
        var result = _controller.ResetPassword("user-id", "token");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ResetPasswordViewModel>(viewResult.Model);
        Assert.Equal("user-id", model.UserId);
        Assert.Equal("token", model.Token);
    }

    [Fact]
    public void ResetPassword_Get_WithMissingParams_RedirectsToHome()
    {
        // Act
        var result = _controller.ResetPassword("", "");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task ResetPassword_Post_WithValidToken_ResetsPasswordAndRedirects()
    {
        // Arrange
        var model = new ResetPasswordViewModel
        {
            UserId = "user-id",
            Token = "valid-token",
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };
        var user = new ApplicationUser { Id = model.UserId, Email = "test@example.com" };

        _userManagerMock.Setup(x => x.FindByIdAsync(model.UserId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, model.Token, model.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.ResetPassword(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ResetPasswordConfirmation", redirectResult.ActionName);
    }

    [Fact]
    public async Task ResetPassword_Post_WithInvalidToken_ReturnsViewWithErrors()
    {
        // Arrange
        var model = new ResetPasswordViewModel
        {
            UserId = "user-id",
            Token = "invalid-token",
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };
        var user = new ApplicationUser { Id = model.UserId };

        _userManagerMock.Setup(x => x.FindByIdAsync(model.UserId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, model.Token, model.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        // Act
        var result = await _controller.ResetPassword(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task ResetPassword_Post_WithNonExistentUser_RedirectsToConfirmation()
    {
        // Arrange (to prevent user enumeration)
        var model = new ResetPasswordViewModel
        {
            UserId = "nonexistent-id",
            Token = "token",
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(model.UserId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.ResetPassword(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ResetPasswordConfirmation", redirectResult.ActionName);
    }

    [Fact]
    public void ResetPasswordConfirmation_ReturnsView()
    {
        // Act
        var result = _controller.ResetPasswordConfirmation();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    #endregion

    #region AccessDenied Tests

    [Fact]
    public void AccessDenied_ReturnsView()
    {
        // Act
        var result = _controller.AccessDenied();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    #endregion
}
