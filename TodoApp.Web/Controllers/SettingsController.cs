using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Web.Models.Entities;
using TodoApp.Web.Models.ViewModels.Settings;
using TodoApp.Web.Services;

namespace TodoApp.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserPreferencesService _preferencesService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        UserManager<ApplicationUser> userManager,
        IUserPreferencesService preferencesService,
        ILogger<SettingsController> logger)
    {
        _userManager = userManager;
        _preferencesService = preferencesService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var preferences = await _preferencesService.GetPreferencesAsync(userId);

        var viewModel = new AccessibilitySettingsViewModel
        {
            FontSize = preferences.FontSize,
            LineSpacing = preferences.LineSpacing,
            HighContrastMode = preferences.HighContrastMode,
            ReducedMotion = preferences.ReducedMotion
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAccessibility(AccessibilitySettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        await _preferencesService.UpdatePreferencesAsync(
            userId,
            model.FontSize,
            model.LineSpacing,
            model.HighContrastMode,
            model.ReducedMotion);

        _logger.LogInformation("User {UserId} updated accessibility settings", userId);

        TempData["SuccessMessage"] = "Accessibility settings saved successfully.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePreferenceAjax([FromBody] UpdatePreferenceRequest request)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            await _preferencesService.UpdatePreferenceAsync(userId, request.Key, request.Value);
            _logger.LogInformation("User {UserId} updated preference {Key} to {Value}", userId, request.Key, request.Value);
            return Json(new { success = true });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    public class UpdatePreferenceRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
