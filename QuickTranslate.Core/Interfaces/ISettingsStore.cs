using QuickTranslate.Core.Models;

namespace QuickTranslate.Core.Interfaces;

/// <summary>
/// Provides persistent storage for application settings with caching.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Loads application settings from persistent storage.
    /// </summary>
    /// <returns>The loaded application settings.</returns>
    AppSettings Load();

    /// <summary>
    /// Saves application settings to persistent storage.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    void Save(AppSettings settings);
}
