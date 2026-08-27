using TodoApp.Web.Models.ViewModels.Task;
using Xunit;

namespace TodoApp.Web.Tests.Models;

public class TaskItemViewModelTests
{
    [Fact]
    public void TimeAgo_JustNow_WhenLessThan60Seconds()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddSeconds(-30)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("just now", result);
    }

    [Fact]
    public void TimeAgo_Minutes_WhenLessThanOneHour()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("5 minutes ago", result);
    }

    [Fact]
    public void TimeAgo_SingleMinute_UsesCorrectGrammar()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddMinutes(-1).AddSeconds(-30)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("1 minute ago", result);
    }

    [Fact]
    public void TimeAgo_Hours_WhenLessThanOneDay()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("3 hours ago", result);
    }

    [Fact]
    public void TimeAgo_SingleHour_UsesCorrectGrammar()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddHours(-1).AddMinutes(-30)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("1 hour ago", result);
    }

    [Fact]
    public void TimeAgo_Days_WhenLessThanOneWeek()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("3 days ago", result);
    }

    [Fact]
    public void TimeAgo_SingleDay_UsesCorrectGrammar()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-12)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("1 day ago", result);
    }

    [Fact]
    public void TimeAgo_Weeks_WhenLessThanOneMonth()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("2 weeks ago", result);
    }

    [Fact]
    public void TimeAgo_SingleWeek_UsesCorrectGrammar()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("1 week ago", result);
    }

    [Fact]
    public void TimeAgo_Months_WhenLessThanOneYear()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("2 months ago", result);
    }

    [Fact]
    public void TimeAgo_SingleMonth_UsesCorrectGrammar()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddDays(-45)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("1 month ago", result);
    }

    [Fact]
    public void TimeAgo_Years_WhenMoreThanOneYear()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddDays(-730)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("2 years ago", result);
    }

    [Fact]
    public void TimeAgo_SingleYear_UsesCorrectGrammar()
    {
        // Arrange
        var viewModel = new TaskItemViewModel
        {
            CreatedAt = DateTime.UtcNow.AddDays(-400)
        };

        // Act
        var result = viewModel.TimeAgo;

        // Assert
        Assert.Equal("1 year ago", result);
    }

    [Fact]
    public void TaskItemViewModel_DefaultValues()
    {
        // Arrange & Act
        var viewModel = new TaskItemViewModel();

        // Assert
        Assert.Equal(0, viewModel.Id);
        Assert.Equal(string.Empty, viewModel.Title);
        Assert.Null(viewModel.Description);
        Assert.False(viewModel.Completed);
        Assert.Equal(default(DateTime), viewModel.CreatedAt);
    }
}
