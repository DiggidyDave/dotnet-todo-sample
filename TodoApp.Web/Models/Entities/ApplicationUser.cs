using Microsoft.AspNetCore.Identity;

namespace TodoApp.Web.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();

    public virtual UserPreferences? Preferences { get; set; }
}
