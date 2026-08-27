using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Web.Data;
using TodoApp.Web.Models.Entities;
using Xunit;

namespace TodoApp.Web.Tests.IntegrationTests;

public class TaskIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TaskIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private async Task<(string userId, string authCookie)> CreateAndLoginUserAsync(string email = "test@example.com")
    {
        // Create a fresh client for each login to avoid shared state issues
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

        // Login via HTTP to get auth cookie
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
    public async Task Create_WithPrefixedFormFields_CreatesTaskSuccessfully()
    {
        // This test verifies the fix for the model binding prefix issue
        // The form uses CreateTask.Title and CreateTask.Description
        // which must be properly bound with [Bind(Prefix = "CreateTask")]

        // Arrange
        var (userId, authCookie) = await CreateAndLoginUserAsync("create-test@example.com");
        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Get the task index page to get anti-forgery token
        var indexPage = await _client.GetAsync("/Task");
        var indexContent = await indexPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(indexContent);

        // Act - Submit the form with the PREFIXED field names (as the actual form does)
        var createForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "CreateTask.Title", "Integration Test Task" },
            { "CreateTask.Description", "This task was created via integration test" }
        });

        var response = await _client.PostAsync("/Task/Create", createForm);

        // Assert - Should redirect to Index (302)
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Task", response.Headers.Location?.ToString());

        // Verify task was actually created in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var task = await context.Tasks.FirstOrDefaultAsync(t => t.Title == "Integration Test Task");

        Assert.NotNull(task);
        Assert.Equal("This task was created via integration test", task.Description);
        Assert.Equal(userId, task.UserId);
        Assert.False(task.Completed);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_DoesNotCreateTask()
    {
        // Arrange
        var (_, authCookie) = await CreateAndLoginUserAsync("empty-title@example.com");
        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        var indexPage = await _client.GetAsync("/Task");
        var indexContent = await indexPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(indexContent);

        // Act - Submit with empty title
        var createForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken },
            { "CreateTask.Title", "" },
            { "CreateTask.Description", "Should not be created" }
        });

        var initialCount = await GetTaskCountAsync();
        var response = await _client.PostAsync("/Task/Create", createForm);
        var finalCount = await GetTaskCountAsync();

        // Assert - Should redirect but not create task
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(initialCount, finalCount);
    }

    [Fact]
    public async Task Toggle_ChangesTaskCompletionStatus()
    {
        // Arrange
        var (userId, authCookie) = await CreateAndLoginUserAsync("toggle-test@example.com");
        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Create a task directly in database
        int taskId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var task = new TodoTask
            {
                Title = "Task to Toggle",
                UserId = userId,
                Completed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
            taskId = task.Id;
        }

        var indexPage = await _client.GetAsync("/Task");
        var indexContent = await indexPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(indexContent);

        // Act - Toggle the task
        var toggleForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken }
        });

        var response = await _client.PostAsync($"/Task/Toggle/{taskId}", toggleForm);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var toggledTask = await verifyContext.Tasks.FindAsync(taskId);
        Assert.True(toggledTask!.Completed);
    }

    [Fact]
    public async Task Toggle_WithAjaxRequest_ReturnsJson()
    {
        // Arrange
        var (userId, authCookie) = await CreateAndLoginUserAsync("toggle-ajax@example.com");
        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        int taskId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var task = new TodoTask
            {
                Title = "AJAX Toggle Task",
                UserId = userId,
                Completed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
            taskId = task.Id;
        }

        var indexPage = await _client.GetAsync("/Task");
        var indexContent = await indexPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(indexContent);

        // Act - Toggle with AJAX header
        var request = new HttpRequestMessage(HttpMethod.Post, $"/Task/Toggle/{taskId}");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Add("RequestVerificationToken", antiForgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken }
        });

        var response = await _client.SendAsync(request);

        // Assert - Should return JSON
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", content);
        Assert.Contains("completed", content);
    }

    [Fact]
    public async Task Delete_RemovesTaskFromDatabase()
    {
        // Arrange
        var (userId, authCookie) = await CreateAndLoginUserAsync("delete-test@example.com");
        _client.DefaultRequestHeaders.Add("Cookie", authCookie);

        int taskId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var task = new TodoTask
            {
                Title = "Task to Delete",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
            taskId = task.Id;
        }

        var indexPage = await _client.GetAsync("/Task");
        var indexContent = await indexPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(indexContent);

        // Act
        var deleteForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken }
        });

        var response = await _client.PostAsync($"/Task/Delete/{taskId}", deleteForm);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deletedTask = await verifyContext.Tasks.FindAsync(taskId);
        Assert.Null(deletedTask);
    }

    [Fact]
    public async Task Index_OnlyShowsCurrentUsersTasks()
    {
        // Arrange - Create two users with tasks
        var (userId1, authCookie1) = await CreateAndLoginUserAsync("user1@example.com");
        var (userId2, _) = await CreateAndLoginUserAsync("user2@example.com");

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Tasks.AddRange(
                new TodoTask { Title = "User 1 Task", UserId = userId1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TodoTask { Title = "User 2 Task", UserId = userId2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Add("Cookie", authCookie1);

        // Act
        var response = await _client.GetAsync("/Task");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - User 1 should only see their task
        Assert.Contains("User 1 Task", content);
        Assert.DoesNotContain("User 2 Task", content);
    }

    [Fact]
    public async Task Toggle_CannotToggleOtherUsersTask()
    {
        // Arrange
        var (userId1, authCookie1) = await CreateAndLoginUserAsync("owner@example.com");
        var (_, authCookie2) = await CreateAndLoginUserAsync("attacker@example.com");

        int taskId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var task = new TodoTask
            {
                Title = "Owner's Task",
                UserId = userId1,
                Completed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
            taskId = task.Id;
        }

        // Login as attacker
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Cookie", authCookie2);

        var indexPage = await _client.GetAsync("/Task");
        var indexContent = await indexPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(indexContent);

        // Act - Try to toggle another user's task
        var toggleForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken }
        });

        var response = await _client.PostAsync($"/Task/Toggle/{taskId}", toggleForm);

        // Assert - Should return NotFound (not allowed to access other user's task)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify task was not modified
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unmodifiedTask = await verifyContext.Tasks.FindAsync(taskId);
        Assert.False(unmodifiedTask!.Completed);
    }

    [Fact]
    public async Task Delete_CannotDeleteOtherUsersTask()
    {
        // Arrange
        var (userId1, _) = await CreateAndLoginUserAsync("task-owner@example.com");
        var (_, authCookie2) = await CreateAndLoginUserAsync("malicious-user@example.com");

        int taskId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var task = new TodoTask
            {
                Title = "Owner's Protected Task",
                UserId = userId1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
            taskId = task.Id;
        }

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Cookie", authCookie2);

        var indexPage = await _client.GetAsync("/Task");
        var indexContent = await indexPage.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(indexContent);

        // Act
        var deleteForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", antiForgeryToken }
        });

        var response = await _client.PostAsync($"/Task/Delete/{taskId}", deleteForm);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify task still exists
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existingTask = await verifyContext.Tasks.FindAsync(taskId);
        Assert.NotNull(existingTask);
    }

    [Fact]
    public async Task TaskIndex_RequiresAuthentication()
    {
        // Arrange - Use a fresh client without auth
        var unauthClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await unauthClient.GetAsync("/Task");

        // Assert - Should redirect to login
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    private async Task<int> GetTaskCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Tasks.CountAsync();
    }
}
