namespace TodoApp.Web.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink);
    Task SendTaskCreatedEmailAsync(string email, string userName, string taskTitle);
}
