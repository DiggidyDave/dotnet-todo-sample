using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TodoApp.Web.Services;
using Xunit;

namespace TodoApp.Web.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly SmtpSettings _smtpSettings;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmailService>>();
        _smtpSettings = new SmtpSettings
        {
            Host = "smtp.test.com",
            Port = 587,
            EnableSsl = true,
            Username = "test@test.com",
            Password = "password",
            FromEmail = "noreply@test.com",
            FromName = "Test App"
        };

        var optionsMock = new Mock<IOptions<SmtpSettings>>();
        optionsMock.Setup(x => x.Value).Returns(_smtpSettings);

        _emailService = new EmailService(optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_DoesNotThrow_WhenSmtpFails()
    {
        // This test verifies that the email service gracefully handles SMTP failures
        // (the actual SMTP will fail because we're using fake credentials)

        // Act & Assert - should not throw
        var exception = await Record.ExceptionAsync(() =>
            _emailService.SendPasswordResetEmailAsync("test@example.com", "http://reset-link"));

        // The method should catch exceptions internally and log them
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendTaskCreatedEmailAsync_DoesNotThrow_WhenSmtpFails()
    {
        // Act & Assert - should not throw
        var exception = await Record.ExceptionAsync(() =>
            _emailService.SendTaskCreatedEmailAsync("test@example.com", "Test User", "Test Task"));

        Assert.Null(exception);
    }

    [Fact]
    public void SmtpSettings_HasCorrectDefaults()
    {
        // Arrange
        var settings = new SmtpSettings();

        // Assert
        Assert.Equal(string.Empty, settings.Host);
        Assert.Equal(0, settings.Port);
        Assert.False(settings.EnableSsl);
        Assert.Equal(string.Empty, settings.Username);
        Assert.Equal(string.Empty, settings.Password);
        Assert.Equal(string.Empty, settings.FromEmail);
        Assert.Equal(string.Empty, settings.FromName);
    }

    [Fact]
    public void SmtpSettings_CanBeConfigured()
    {
        // Arrange & Act
        var settings = new SmtpSettings
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            Username = "user@gmail.com",
            Password = "app-password",
            FromEmail = "noreply@app.com",
            FromName = "My App"
        };

        // Assert
        Assert.Equal("smtp.gmail.com", settings.Host);
        Assert.Equal(587, settings.Port);
        Assert.True(settings.EnableSsl);
        Assert.Equal("user@gmail.com", settings.Username);
        Assert.Equal("app-password", settings.Password);
        Assert.Equal("noreply@app.com", settings.FromEmail);
        Assert.Equal("My App", settings.FromName);
    }
}
