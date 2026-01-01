using System.Windows.Media;

namespace QuickTranslate.Desktop.Converters;

/// <summary>
/// Theme-aware color definitions for converters.
/// These colors can be customized to match different themes.
/// </summary>
public static class ThemeColors
{
    /// <summary>
    /// Color used to display error states.
    /// </summary>
    public static readonly Color Error = Color.FromRgb(255, 100, 100);

    /// <summary>
    /// Default neutral color for UI elements.
    /// </summary>
    public static readonly Color Neutral = Color.FromRgb(136, 136, 136);

    /// <summary>
    /// Color used to indicate TTS is speaking.
    /// </summary>
    public static readonly Color Speaking = Color.FromRgb(76, 175, 80);

    /// <summary>
    /// Color used to highlight favorite items.
    /// </summary>
    public static readonly Color Favorite = Color.FromRgb(255, 193, 7);

    /// <summary>
    /// Color used to indicate provider is healthy.
    /// </summary>
    public static readonly Color Healthy = Color.FromRgb(76, 175, 80);

    /// <summary>
    /// Color used to indicate provider is unhealthy or degraded.
    /// </summary>
    public static readonly Color Unhealthy = Color.FromRgb(255, 100, 100);

    /// <summary>
    /// Color used to indicate provider status is unknown or checking.
    /// </summary>
    public static readonly Color Unknown = Color.FromRgb(255, 193, 7);
}
