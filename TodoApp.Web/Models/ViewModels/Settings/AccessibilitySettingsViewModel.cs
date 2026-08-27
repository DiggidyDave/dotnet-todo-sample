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

    [Required]
    [Display(Name = "Theme")]
    public string Theme { get; set; } = "default";

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

    public static readonly Dictionary<string, (string Name, string Description)> ThemeOptions = new()
    {
        { "default", ("Light", "Clean light theme") },
        { "dark", ("Dark", "Easy on the eyes") },
        { "high-contrast-light", ("High Contrast Light", "Black on white, maximum readability") },
        { "high-contrast-dark", ("High Contrast Dark", "White on black, maximum readability") },
        { "ocean", ("Ocean", "Calming blues and teals") },
        { "forest", ("Forest", "Natural greens") },
        { "sunset", ("Sunset", "Warm oranges and reds") },
        { "lavender", ("Lavender", "Soft purples") }
    };
}
