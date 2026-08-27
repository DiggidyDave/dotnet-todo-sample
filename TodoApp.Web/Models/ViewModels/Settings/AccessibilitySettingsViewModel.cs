using System.ComponentModel.DataAnnotations;

namespace TodoApp.Web.Models.ViewModels.Settings;

public class AccessibilitySettingsViewModel
{
    [Required]
    [Display(Name = "Font Size")]
    public string FontSize { get; set; } = "medium";

    [Required]
    [Display(Name = "Line Spacing")]
    public string LineSpacing { get; set; } = "normal";

    [Display(Name = "High Contrast Mode")]
    public bool HighContrastMode { get; set; }

    [Display(Name = "Reduced Motion")]
    public bool ReducedMotion { get; set; }

    public static readonly Dictionary<string, string> FontSizeOptions = new()
    {
        { "small", "Small" },
        { "medium", "Medium" },
        { "large", "Large" },
        { "extra-large", "Extra Large" }
    };

    public static readonly Dictionary<string, string> LineSpacingOptions = new()
    {
        { "compact", "Compact" },
        { "normal", "Normal" },
        { "relaxed", "Relaxed" }
    };
}
