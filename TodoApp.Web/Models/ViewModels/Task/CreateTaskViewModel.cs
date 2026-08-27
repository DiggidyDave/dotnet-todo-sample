using System.ComponentModel.DataAnnotations;

namespace TodoApp.Web.Models.ViewModels.Task;

public class CreateTaskViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title must be between {2} and {1} characters", MinimumLength = 1)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed {1} characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
}
