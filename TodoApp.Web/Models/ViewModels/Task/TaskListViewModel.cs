namespace TodoApp.Web.Models.ViewModels.Task;

public class TaskListViewModel
{
    public List<TaskItemViewModel> Tasks { get; set; } = new();
    public CreateTaskViewModel CreateTask { get; set; } = new();
    public string Filter { get; set; } = "all";
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int CompletedCount { get; set; }
}
