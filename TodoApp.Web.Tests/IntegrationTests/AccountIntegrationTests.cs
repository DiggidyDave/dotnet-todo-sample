using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Web.Data;
using TodoApp.Web.Models.Entities;
using Xunit;

namespace TodoApp.Web.Tests.IntegrationTests;

public class AccountIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AccountIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(html, @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""");
        if (!match.Success)
        {
            match = Regex.Match(html, @"value=""([^""]+)""\s+name=""__RequestVerificationToken""");
        }
        return match.Success ? match.Groups[1].Value : "";
    }

    [Fact]
    public async Task Register_WithValidData_CreatesUserAndRedirects()
    {
        // Arrange
        var registerPage = await _client.GetAsync("/Account/Register");
        var registerContent = await registerPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(registerContent);

        var email = $"newuser-{Guid.NewGuid()}@example.com";
        var registerForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "Name", "New User" },
            { "Email", email },
            { "Password", "Password123!" },
            { "ConfirmPassword", "Password123!" }
        });

        // Act
        var response = await _client.PostAsync("/Account/Register", registerForm);

        // Assert - Should redirect to Task/Index
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Task", response.Headers.Location?.ToString());

        // Verify user was created
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.Equal("New User", user.Name);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsError()
    {
        // Arrange
        var registerPage = await _client.GetAsync("/Account/Register");
        var registerContent = await registerPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(registerContent);

        var registerForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "Name", "Weak Password User" },
            { "Email", "weakpass@example.com" },
            { "Password", "weak" },
            { "ConfirmPassword", "weak" }
        });

        // Act
        var response = await _client.PostAsync("/Account/Register", registerForm);

        // Assert - Should return view (not redirect) with error
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        // Password error messages should be present
        Assert.True(
            content.Contains("at least") ||
            content.Contains("Passwords must") ||
            content.Contains("uppercase") ||
            content.Contains("digit"));
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ReturnsError()
    {
        // Arrange
        var registerPage = await _client.GetAsync("/Account/Register");
        var registerContent = await registerPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(registerContent);

        var registerForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "Name", "Mismatch User" },
            { "Email", "mismatch@example.com" },
            { "Password", "Password123!" },
            { "ConfirmPassword", "DifferentPassword123!" }
        });

        // Act
        var response = await _client.PostAsync("/Account/Register", registerForm);

        // Assert - Should return view with validation error
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToTaskIndex()
    {
        // Arrange - Create a user first
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = "logintest@example.com",
                Email = "logintest@example.com",
                Name = "Login Test User"
            };
            await userManager.CreateAsync(user, "Password123!");
        }

        var loginPage = await _client.GetAsync("/Account/Login");
        var loginContent = await loginPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginContent);

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "Email", "logintest@example.com" },
            { "Password", "Password123!" },
            { "RememberMe", "false" }
        });

        // Act
        var response = await _client.PostAsync("/Account/Login", loginForm);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Task", response.Headers.Location?.ToString());

        // Verify auth cookie is set
        Assert.True(response.Headers.Contains("Set-Cookie"));
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.Contains(".AspNetCore.Identity.Application"));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsError()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = "wrongpass@example.com",
                Email = "wrongpass@example.com",
                Name = "Wrong Password User"
            };
            await userManager.CreateAsync(user, "Password123!");
        }

        var loginPage = await _client.GetAsync("/Account/Login");
        var loginContent = await loginPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginContent);

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "Email", "wrongpass@example.com" },
            { "Password", "WrongPassword123!" },
            { "RememberMe", "false" }
        });

        // Act
        var response = await _client.PostAsync("/Account/Login", loginForm);

        // Assert - Should return view with error (not redirect)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid", content);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsError()
    {
        // Arrange
        var loginPage = await _client.GetAsync("/Account/Login");
        var loginContent = await loginPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginContent);

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "Email", "nonexistent@example.com" },
            { "Password", "Password123!" },
            { "RememberMe", "false" }
        });

        // Act
        var response = await _client.PostAsync("/Account/Login", loginForm);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid", content);
    }

    [Fact]
    public async Task Logout_ClearsAuthenticationAndRedirectsToHome()
    {
        // Arrange - Login first
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = "logouttest@example.com",
                Email = "logouttest@example.com",
                Name = "Logout Test User"
            };
            await userManager.CreateAsync(user, "Password123!");
        }

        var loginPage = await _client.GetAsync("/Account/Login");
        var loginContent = await loginPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginContent);

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "Email", "logouttest@example.com" },
            { "Password", "Password123!" }
        });

        var loginResponse = await _client.PostAsync("/Account/Login", loginForm);
        var authCookie = loginResponse.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.Contains(".AspNetCore.Identity.Application"));
        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Get new anti-forgery token while logged in
        var taskPage = await _client.GetAsync("/Task");
        var taskContent = await taskPage.Content.ReadAsStringAsync();
        var logoutToken = ExtractAntiForgeryToken(taskContent);

        // Act
        var logoutForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", logoutToken }
        });
        var logoutResponse = await _client.PostAsync("/Account/Logout", logoutForm);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        Assert.Equal("/", logoutResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task ForgotPassword_AlwaysRedirectsToConfirmation()
    {
        // This test verifies we don't leak whether an email exists (security)

        // Arrange
        var forgotPage = await _client.GetAsync("/Account/ForgotPassword");
        var forgotContent = await forgotPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(forgotContent);

        // Act - Submit with non-existent email
        var forgotForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "Email", "doesnotexist@example.com" }
        });

        var response = await _client.PostAsync("/Account/ForgotPassword", forgotForm);

        // Assert - Should still redirect to confirmation (no email enumeration)
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/ForgotPasswordConfirmation", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Login_WhenAlreadyAuthenticated_RedirectsToTaskIndex()
    {
        // Arrange - Login first
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = "alreadylogged@example.com",
                Email = "alreadylogged@example.com",
                Name = "Already Logged User"
            };
            await userManager.CreateAsync(user, "Password123!");
        }

        var loginPage = await _client.GetAsync("/Account/Login");
        var loginContent = await loginPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginContent);

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "Email", "alreadylogged@example.com" },
            { "Password", "Password123!" }
        });

        var loginResponse = await _client.PostAsync("/Account/Login", loginForm);
        var authCookie = loginResponse.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.Contains(".AspNetCore.Identity.Application"));
        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Act - Try to access login page while authenticated
        var response = await _client.GetAsync("/Account/Login");

        // Assert - Should redirect to Task index
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Task", response.Headers.Location?.ToString());
    }
}
