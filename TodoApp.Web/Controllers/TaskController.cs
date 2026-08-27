using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Web.Data;
using TodoApp.Web.Models.Entities;
using TodoApp.Web.Models.ViewModels.Task;
using TodoApp.Web.Services;

namespace TodoApp.Web.Controllers;

[Authorize]
public class TaskController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<TaskController> _logger;

    public TaskController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<TaskController> logger)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string filter = "all")
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var tasksQuery = _context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt);

        var allTasks = await tasksQuery.ToListAsync();
        var filteredTasks = filter switch
        {
            "active" => allTasks.Where(t => !t.Completed).ToList(),
            "completed" => allTasks.Where(t => t.Completed).ToList(),
            _ => allTasks
        };

        var viewModel = new TaskListViewModel
        {
            Tasks = filteredTasks.Select(t => new TaskItemViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Completed = t.Completed,
                CreatedAt = t.CreatedAt
            }).ToList(),
            Filter = filter,
            TotalCount = allTasks.Count,
            ActiveCount = allTasks.Count(t => !t.Completed),
            CompletedCount = allTasks.Count(t => t.Completed)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTaskViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Index");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var task = new TodoTask
        {
            Title = model.Title,
            Description = model.Description,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Task created by user {UserId}: {TaskTitle}", user.Id, task.Title);

        // Send email notification (async, don't wait)
        _ = _emailService.SendTaskCreatedEmailAsync(user.Email!, user.Name, task.Title);

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var userId = _userManager.GetUserId(User);
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        task.Completed = !task.Completed;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} toggled to {Completed}", id, task.Completed);

        // Return JSON for AJAX requests
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return Json(new { success = true, completed = task.Completed });
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} deleted", id);

        // Return JSON for AJAX requests
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return Json(new { success = true });
        }

        return RedirectToAction("Index");
    }
}
