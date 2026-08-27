using Microsoft.EntityFrameworkCore;
using TodoApp.Web.Data;
using TodoApp.Web.Models.Entities;

namespace TodoApp.Web.Services;

public class UserPreferencesService : IUserPreferencesService
{
    private readonly ApplicationDbContext _context;

    public UserPreferencesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreferences> GetPreferencesAsync(string userId)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        return preferences ?? new UserPreferences { UserId = userId };
    }

    public async Task<UserPreferences> GetOrCreatePreferencesAsync(string userId)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            preferences = new UserPreferences { UserId = userId };
            _context.UserPreferences.Add(preferences);
            await _context.SaveChangesAsync();
        }

        return preferences;
    }

    public async Task UpdatePreferencesAsync(string userId, string fontSize, string lineSpacing, bool highContrastMode, bool reducedMotion)
    {
        var preferences = await GetOrCreatePreferencesAsync(userId);

        preferences.FontSize = fontSize;
        preferences.LineSpacing = lineSpacing;
        preferences.HighContrastMode = highContrastMode;
        preferences.ReducedMotion = reducedMotion;
        preferences.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task UpdatePreferenceAsync(string userId, string key, string value)
    {
        var preferences = await GetOrCreatePreferencesAsync(userId);

        switch (key.ToLowerInvariant())
        {
            case "fontsize":
                preferences.FontSize = value;
                break;
            case "linespacing":
                preferences.LineSpacing = value;
                break;
            case "highcontrastmode":
                preferences.HighContrastMode = bool.Parse(value);
                break;
            case "reducedmotion":
                preferences.ReducedMotion = bool.Parse(value);
                break;
            default:
                throw new ArgumentException($"Unknown preference key: {key}");
        }

        preferences.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
