using Microsoft.EntityFrameworkCore;
using TodoApp.Web.Data;
using TodoApp.Web.Models.Entities;
using TodoApp.Web.Services;
using Xunit;

namespace TodoApp.Web.Tests.Services;

public class UserPreferencesServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserPreferencesService _service;
    private readonly string _testUserId = "test-user-id";

    public UserPreferencesServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new UserPreferencesService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetPreferencesAsync_ReturnsDefaultPreferences_WhenNoneExist()
    {
        // Act
        var result = await _service.GetPreferencesAsync(_testUserId);

        // Assert
        Assert.Equal(_testUserId, result.UserId);
        Assert.Equal("medium", result.FontSize);
        Assert.Equal("normal", result.LineSpacing);
        Assert.Equal("default", result.Theme);
        Assert.False(result.ReducedMotion);
    }

    [Fact]
    public async Task GetPreferencesAsync_ReturnsExistingPreferences()
    {
        // Arrange
        var preferences = new UserPreferences
        {
            UserId = _testUserId,
            FontSize = "large",
            LineSpacing = "relaxed",
            Theme = "dark",
            ReducedMotion = true
        };
        _context.UserPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPreferencesAsync(_testUserId);

        // Assert
        Assert.Equal("large", result.FontSize);
        Assert.Equal("relaxed", result.LineSpacing);
        Assert.Equal("dark", result.Theme);
        Assert.True(result.ReducedMotion);
    }

    [Fact]
    public async Task GetOrCreatePreferencesAsync_CreatesNewPreferences_WhenNoneExist()
    {
        // Act
        var result = await _service.GetOrCreatePreferencesAsync(_testUserId);

        // Assert
        Assert.Equal(_testUserId, result.UserId);
        Assert.Equal("medium", result.FontSize);

        var dbPreferences = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == _testUserId);
        Assert.NotNull(dbPreferences);
    }

    [Fact]
    public async Task GetOrCreatePreferencesAsync_ReturnsExisting_WhenExists()
    {
        // Arrange
        var existing = new UserPreferences
        {
            UserId = _testUserId,
            FontSize = "small"
        };
        _context.UserPreferences.Add(existing);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetOrCreatePreferencesAsync(_testUserId);

        // Assert
        Assert.Equal("small", result.FontSize);
        Assert.Single(_context.UserPreferences);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_CreatesAndUpdatesPreferences()
    {
        // Act
        await _service.UpdatePreferencesAsync(_testUserId, "extra-large", "compact", "high-contrast-dark", true);

        // Assert
        var result = await _context.UserPreferences.FirstAsync(p => p.UserId == _testUserId);
        Assert.Equal("extra-large", result.FontSize);
        Assert.Equal("compact", result.LineSpacing);
        Assert.Equal("high-contrast-dark", result.Theme);
        Assert.True(result.ReducedMotion);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_UpdatesExistingPreferences()
    {
        // Arrange
        var existing = new UserPreferences
        {
            UserId = _testUserId,
            FontSize = "small",
            LineSpacing = "normal"
        };
        _context.UserPreferences.Add(existing);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdatePreferencesAsync(_testUserId, "large", "relaxed", "ocean", false);

        // Assert
        var result = await _context.UserPreferences.FirstAsync(p => p.UserId == _testUserId);
        Assert.Equal("large", result.FontSize);
        Assert.Equal("relaxed", result.LineSpacing);
        Assert.Equal("ocean", result.Theme);
        Assert.False(result.ReducedMotion);
        Assert.Single(_context.UserPreferences);
    }

    [Theory]
    [InlineData("fontsize", "small")]
    [InlineData("fontsize", "large")]
    [InlineData("linespacing", "compact")]
    [InlineData("linespacing", "relaxed")]
    [InlineData("theme", "dark")]
    [InlineData("theme", "ocean")]
    public async Task UpdatePreferenceAsync_UpdatesStringPreference(string key, string value)
    {
        // Arrange
        var existing = new UserPreferences { UserId = _testUserId };
        _context.UserPreferences.Add(existing);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdatePreferenceAsync(_testUserId, key, value);

        // Assert
        var result = await _context.UserPreferences.FirstAsync(p => p.UserId == _testUserId);
        if (key == "fontsize")
            Assert.Equal(value, result.FontSize);
        else if (key == "linespacing")
            Assert.Equal(value, result.LineSpacing);
        else if (key == "theme")
            Assert.Equal(value, result.Theme);
    }

    [Theory]
    [InlineData("reducedmotion", "true", true)]
    [InlineData("reducedmotion", "false", false)]
    public async Task UpdatePreferenceAsync_UpdatesBooleanPreference(string key, string value, bool expected)
    {
        // Arrange
        var existing = new UserPreferences { UserId = _testUserId };
        _context.UserPreferences.Add(existing);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdatePreferenceAsync(_testUserId, key, value);

        // Assert
        var result = await _context.UserPreferences.FirstAsync(p => p.UserId == _testUserId);
        Assert.Equal(expected, result.ReducedMotion);
    }

    [Fact]
    public async Task UpdatePreferenceAsync_ThrowsForUnknownKey()
    {
        // Arrange
        var existing = new UserPreferences { UserId = _testUserId };
        _context.UserPreferences.Add(existing);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdatePreferenceAsync(_testUserId, "unknownkey", "value"));
    }

    [Fact]
    public async Task UpdatePreferenceAsync_UpdatesTimestamp()
    {
        // Arrange
        var existing = new UserPreferences
        {
            UserId = _testUserId,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.UserPreferences.Add(existing);
        await _context.SaveChangesAsync();
        var originalTimestamp = existing.UpdatedAt;

        // Act
        await _service.UpdatePreferenceAsync(_testUserId, "fontsize", "large");

        // Assert
        var result = await _context.UserPreferences.FirstAsync(p => p.UserId == _testUserId);
        Assert.True(result.UpdatedAt > originalTimestamp);
    }
}
