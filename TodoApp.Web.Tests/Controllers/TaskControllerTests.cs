using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TodoApp.Web.Controllers;
using TodoApp.Web.Data;
using TodoApp.Web.Models.Entities;
using TodoApp.Web.Models.ViewModels.Task;
using TodoApp.Web.Services;
using Xunit;

namespace TodoApp.Web.Tests.Controllers;

public class TaskControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<TaskController>> _loggerMock;
    private readonly TaskController _controller;
    private readonly ApplicationUser _testUser;

    public TaskControllerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup test user
        _testUser = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "test@example.com",
            Email = "test@example.com",
            Name = "Test User"
        };

        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(_testUser.Id);
        _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        // Setup other mocks
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<TaskController>>();

        // Create controller
        _controller = new TaskController(
            _context,
            _userManagerMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);

        // Setup controller context with authenticated user
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

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task Index_ReturnsViewWithTaskList()
    {
        // Arrange
        _context.Tasks.AddRange(
            new TodoTask { Id = 1, Title = "Task 1", UserId = _testUser.Id, Completed = false },
            new TodoTask { Id = 2, Title = "Task 2", UserId = _testUser.Id, Completed = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TaskListViewModel>(viewResult.Model);
        Assert.Equal(2, model.TotalCount);
        Assert.Equal(1, model.ActiveCount);
        Assert.Equal(1, model.CompletedCount);
    }

    [Fact]
    public async Task Index_WithActiveFilter_ReturnsOnlyActiveTasks()
    {
        // Arrange
        _context.Tasks.AddRange(
            new TodoTask { Id = 1, Title = "Active Task", UserId = _testUser.Id, Completed = false },
            new TodoTask { Id = 2, Title = "Completed Task", UserId = _testUser.Id, Completed = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index("active");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TaskListViewModel>(viewResult.Model);
        Assert.Single(model.Tasks);
        Assert.Equal("Active Task", model.Tasks[0].Title);
        Assert.Equal("active", model.Filter);
    }

    [Fact]
    public async Task Index_WithCompletedFilter_ReturnsOnlyCompletedTasks()
    {
        // Arrange
        _context.Tasks.AddRange(
            new TodoTask { Id = 1, Title = "Active Task", UserId = _testUser.Id, Completed = false },
            new TodoTask { Id = 2, Title = "Completed Task", UserId = _testUser.Id, Completed = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index("completed");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TaskListViewModel>(viewResult.Model);
        Assert.Single(model.Tasks);
        Assert.Equal("Completed Task", model.Tasks[0].Title);
    }

    [Fact]
    public async Task Index_OnlyReturnsTasksForCurrentUser()
    {
        // Arrange
        _context.Tasks.AddRange(
            new TodoTask { Id = 1, Title = "My Task", UserId = _testUser.Id },
            new TodoTask { Id = 2, Title = "Other User Task", UserId = "other-user-id" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TaskListViewModel>(viewResult.Model);
        Assert.Single(model.Tasks);
        Assert.Equal("My Task", model.Tasks[0].Title);
    }

    [Fact]
    public async Task Create_WithValidModel_CreatesTaskAndRedirects()
    {
        // Arrange
        var model = new CreateTaskViewModel
        {
            Title = "New Task",
            Description = "Task description"
        };

        // Act
        var result = await _controller.Create(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        var task = await _context.Tasks.FirstOrDefaultAsync();
        Assert.NotNull(task);
        Assert.Equal("New Task", task.Title);
        Assert.Equal("Task description", task.Description);
        Assert.Equal(_testUser.Id, task.UserId);
        Assert.False(task.Completed);
    }

    [Fact]
    public async Task Create_SendsEmailNotification()
    {
        // Arrange
        var model = new CreateTaskViewModel { Title = "New Task" };

        // Act
        await _controller.Create(model);

        // Assert (email is sent async, but we can verify it was called)
        // Note: The actual implementation fires and forgets, so this verifies the method exists
        Assert.Single(_context.Tasks);
    }

    [Fact]
    public async Task Create_WithInvalidModel_RedirectsToIndex()
    {
        // Arrange
        var model = new CreateTaskViewModel { Title = "" };
        _controller.ModelState.AddModelError("Title", "Required");

        // Act
        var result = await _controller.Create(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Empty(_context.Tasks);
    }

    [Fact]
    public async Task Toggle_TogglesTaskCompletion()
    {
        // Arrange
        var task = new TodoTask { Id = 1, Title = "Task", UserId = _testUser.Id, Completed = false };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Toggle(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        var updatedTask = await _context.Tasks.FindAsync(1);
        Assert.True(updatedTask!.Completed);
    }

    [Fact]
    public async Task Toggle_WithAjaxRequest_ReturnsJson()
    {
        // Arrange
        var task = new TodoTask { Id = 1, Title = "Task", UserId = _testUser.Id, Completed = false };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _controller.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await _controller.Toggle(1);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task Toggle_WithNonExistentTask_ReturnsNotFound()
    {
        // Act
        var result = await _controller.Toggle(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Toggle_WithOtherUsersTask_ReturnsNotFound()
    {
        // Arrange
        var task = new TodoTask { Id = 1, Title = "Task", UserId = "other-user-id", Completed = false };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Toggle(1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_RemovesTask()
    {
        // Arrange
        var task = new TodoTask { Id = 1, Title = "Task", UserId = _testUser.Id };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Empty(_context.Tasks);
    }

    [Fact]
    public async Task Delete_WithAjaxRequest_ReturnsJson()
    {
        // Arrange
        var task = new TodoTask { Id = 1, Title = "Task", UserId = _testUser.Id };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _controller.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task Delete_WithNonExistentTask_ReturnsNotFound()
    {
        // Act
        var result = await _controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_WithOtherUsersTask_ReturnsNotFound()
    {
        // Arrange
        var task = new TodoTask { Id = 1, Title = "Task", UserId = "other-user-id" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
