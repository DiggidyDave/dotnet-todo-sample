using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApp.Web.Models.Entities;

public class UserPreferences
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser User { get; set; } = null!;

    [MaxLength(20)]
    public string FontSize { get; set; } = "medium";

    [MaxLength(20)]
    public string LineSpacing { get; set; } = "normal";

    [MaxLength(30)]
    public string Theme { get; set; } = "default";

    public bool ReducedMotion { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
