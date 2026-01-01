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
}
