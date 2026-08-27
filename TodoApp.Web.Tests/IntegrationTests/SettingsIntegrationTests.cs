using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Web.Data;
using TodoApp.Web.Models.Entities;
using Xunit;

namespace TodoApp.Web.Tests.IntegrationTests;

public class SettingsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SettingsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private async Task<(string userId, string authCookie)> CreateAndLoginUserAsync(string email)
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = "Test User"
        };

        await userManager.CreateAsync(user, "Password123!");

        var loginPage = await client.GetAsync("/Account/Login");
        var loginContent = await loginPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginContent);

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "Email", email },
            { "Password", "Password123!" },
            { "RememberMe", "false" }
        });

        var loginResponse = await client.PostAsync("/Account/Login", loginForm);

        string authCookie = "";
        if (loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            authCookie = cookies.FirstOrDefault(c => c.Contains(".AspNetCore.Identity.Application")) ?? "";
        }

        return (user.Id, authCookie);
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
    public async Task Settings_RequiresAuthentication()
    {
        // Arrange - Use a fresh client without auth
        var unauthClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await unauthClient.GetAsync("/Settings");

        // Assert - Should redirect to login
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Settings_Index_ReturnsSuccessForAuthenticatedUser()
    {
        // Arrange
        var (_, authCookie) = await CreateAndLoginUserAsync("settings-index@example.com");
        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Act
        var response = await _client.GetAsync("/Settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Accessibility", content);
        Assert.Contains("Font Size", content);
        Assert.Contains("Line Spacing", content);
        Assert.Contains("High Contrast Mode", content);
        Assert.Contains("Reduced Motion", content);
    }

    [Fact]
    public async Task Settings_UpdateAccessibility_SavesPreferences()
    {
        // Arrange
        var (userId, authCookie) = await CreateAndLoginUserAsync("settings-update@example.com");
        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        var settingsPage = await _client.GetAsync("/Settings");
        var settingsContent = await settingsPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(settingsContent);

        // Act
        var updateForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "FontSize", "large" },
            { "LineSpacing", "relaxed" },
            { "HighContrastMode", "true" },
            { "ReducedMotion", "true" }
        });

        var response = await _client.PostAsync("/Settings/UpdateAccessibility", updateForm);

        // Assert - Should redirect back to Settings
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Settings", response.Headers.Location?.ToString());

        // Verify preferences were saved in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var preferences = await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.Equal("large", preferences.FontSize);
        Assert.Equal("relaxed", preferences.LineSpacing);
        Assert.True(preferences.HighContrastMode);
        Assert.True(preferences.ReducedMotion);
    }

    [Fact]
    public async Task Settings_ShowsSuccessMessage_AfterSaving()
    {
        // Arrange
        var (_, authCookie) = await CreateAndLoginUserAsync("settings-message@example.com");
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true  // Follow redirects to see the message
        });
        client.DefaultRequestHeaders.Add("Cookie", authCookie);

        var settingsPage = await client.GetAsync("/Settings");
        var settingsContent = await settingsPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(settingsContent);

        // Act
        var updateForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "FontSize", "medium" },
            { "LineSpacing", "normal" },
            { "HighContrastMode", "false" },
            { "ReducedMotion", "false" }
        });

        var response = await client.PostAsync("/Settings/UpdateAccessibility", updateForm);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Accessibility settings saved successfully", content);
    }

    [Fact]
    public async Task Settings_PreservesExistingPreferences()
    {
        // Arrange - Create user with existing preferences
        var (userId, authCookie) = await CreateAndLoginUserAsync("settings-preserve@example.com");

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.UserPreferences.Add(new UserPreferences
            {
                UserId = userId,
                FontSize = "extra-large",
                LineSpacing = "compact",
                HighContrastMode = true,
                ReducedMotion = true
            });
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Act
        var response = await _client.GetAsync("/Settings");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Form should show existing values (checked attributes)
        Assert.Contains("extra-large", content);
        Assert.Contains("compact", content);
    }

    [Fact]
    public async Task Layout_AppliesUserPreferences()
    {
        // Arrange - Create user with custom preferences
        var (userId, authCookie) = await CreateAndLoginUserAsync("layout-prefs@example.com");

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.UserPreferences.Add(new UserPreferences
            {
                UserId = userId,
                FontSize = "large",
                LineSpacing = "relaxed",
                HighContrastMode = true,
                ReducedMotion = true
            });
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Act - Visit any page
        var response = await _client.GetAsync("/Task");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - HTML should have data attributes
        Assert.Contains("data-font-size=\"large\"", content);
        Assert.Contains("data-line-spacing=\"relaxed\"", content);
        Assert.Contains("data-high-contrast=\"true\"", content);
        Assert.Contains("data-reduced-motion=\"true\"", content);
    }

    [Fact]
    public async Task Layout_UsesDefaultsForNewUser()
    {
        // Arrange - Create user without preferences
        var (_, authCookie) = await CreateAndLoginUserAsync("new-user-defaults@example.com");
        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Act
        var response = await _client.GetAsync("/Task");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Should have default values
        Assert.Contains("data-font-size=\"medium\"", content);
        Assert.Contains("data-line-spacing=\"normal\"", content);
        // High contrast and reduced motion should not be present (false = no attribute)
    }

    [Fact]
    public async Task Navbar_ShowsSettingsLink_WhenLoggedIn()
    {
        // Arrange
        var (_, authCookie) = await CreateAndLoginUserAsync("navbar-settings@example.com");
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true
        });
        client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Act - Use Task page which requires auth and will show full layout
        var response = await client.GetAsync("/Task");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("Settings", content);
        Assert.Contains("/Settings", content);
    }

    [Fact]
    public async Task Settings_CanUpdateIndividualPreferences()
    {
        // Arrange
        var (userId, authCookie) = await CreateAndLoginUserAsync("ajax-update@example.com");

        // Create initial preferences
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.UserPreferences.Add(new UserPreferences
            {
                UserId = userId,
                FontSize = "medium",
                LineSpacing = "normal"
            });
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Get anti-forgery token
        var settingsPage = await _client.GetAsync("/Settings");
        var settingsContent = await settingsPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(settingsContent);

        // Act - Update just font size via AJAX endpoint
        var request = new HttpRequestMessage(HttpMethod.Post, "/Settings/UpdatePreferenceAjax");
        request.Headers.Add("RequestVerificationToken", antiForgeryToken);
        request.Content = new StringContent(
            "{\"key\":\"fontsize\",\"value\":\"extra-large\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", responseContent);

        // Verify in database
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var prefs = await verifyContext.UserPreferences.FirstAsync(p => p.UserId == userId);
        Assert.Equal("extra-large", prefs.FontSize);
        Assert.Equal("normal", prefs.LineSpacing); // Should be unchanged
    }
}
