using TodoApp.Web.Models.Entities;

namespace TodoApp.Web.Services;

public interface IUserPreferencesService
{
    Task<UserPreferences> GetPreferencesAsync(string userId);
    Task<UserPreferences> GetOrCreatePreferencesAsync(string userId);
    Task UpdatePreferencesAsync(string userId, string fontSize, string lineSpacing, bool highContrastMode, bool reducedMotion);
    Task UpdatePreferenceAsync(string userId, string key, string value);
}
