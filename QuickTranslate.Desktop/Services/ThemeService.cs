using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace QuickTranslate.Desktop.Services;

public enum AppTheme
{
    OceanBlue,
    Emerald,
    Sunset,
    Purple,
    Monochrome
}

public class ThemeInfo
{
    public AppTheme Theme { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Color AccentColor { get; set; }
    public Color PrimaryAccent { get; set; }
    public Color SecondaryAccent { get; set; }
    public Color TertiaryAccent { get; set; }
}

public class ThemeService : INotifyPropertyChanged
{
    private static readonly Lazy<ThemeService> _instance = new(() => new ThemeService());
    public static ThemeService Instance => _instance.Value;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ThemeChanged;

    private AppTheme _currentTheme = AppTheme.OceanBlue;

    public static readonly Dictionary<AppTheme, ThemeInfo> AvailableThemes = new()
    {
        { AppTheme.OceanBlue, new ThemeInfo { 
            Theme = AppTheme.OceanBlue, 
            Key = "OceanBlue", 
            DisplayName = "Ocean Blue", 
            AccentColor = Color.FromRgb(0x00, 0x78, 0xD4),
            PrimaryAccent = Color.FromRgb(0x00, 0x67, 0xC0),
            SecondaryAccent = Color.FromRgb(0x00, 0x3E, 0x92),
            TertiaryAccent = Color.FromRgb(0x00, 0x1A, 0x68)
        }},
        { AppTheme.Emerald, new ThemeInfo { 
            Theme = AppTheme.Emerald, 
            Key = "Emerald", 
            DisplayName = "Emerald", 
            AccentColor = Color.FromRgb(0x10, 0xB9, 0x81),
            PrimaryAccent = Color.FromRgb(0x0D, 0x9B, 0x6C),
            SecondaryAccent = Color.FromRgb(0x08, 0x6F, 0x4D),
            TertiaryAccent = Color.FromRgb(0x04, 0x47, 0x31)
        }},
        { AppTheme.Sunset, new ThemeInfo { 
            Theme = AppTheme.Sunset, 
            Key = "Sunset", 
            DisplayName = "Sunset", 
            AccentColor = Color.FromRgb(0xF9, 0x73, 0x16),
            PrimaryAccent = Color.FromRgb(0xEA, 0x58, 0x0C),
            SecondaryAccent = Color.FromRgb(0xC2, 0x41, 0x0C),
            TertiaryAccent = Color.FromRgb(0x9A, 0x34, 0x12)
        }},
        { AppTheme.Purple, new ThemeInfo { 
            Theme = AppTheme.Purple, 
            Key = "Purple", 
            DisplayName = "Purple Nebula", 
            AccentColor = Color.FromRgb(0x8B, 0x5C, 0xF6),
            PrimaryAccent = Color.FromRgb(0x7C, 0x3A, 0xED),
            SecondaryAccent = Color.FromRgb(0x6D, 0x28, 0xD9),
            TertiaryAccent = Color.FromRgb(0x5B, 0x21, 0xB6)
        }},
        { AppTheme.Monochrome, new ThemeInfo { 
            Theme = AppTheme.Monochrome, 
            Key = "Monochrome", 
            DisplayName = "Monochrome", 
            AccentColor = Color.FromRgb(0x64, 0x74, 0x8B),
            PrimaryAccent = Color.FromRgb(0x47, 0x55, 0x69),
            SecondaryAccent = Color.FromRgb(0x33, 0x41, 0x55),
            TertiaryAccent = Color.FromRgb(0x1E, 0x29, 0x3B)
        }}
    };

    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTheme)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentThemeInfo)));
            }
        }
    }

    public ThemeInfo CurrentThemeInfo => AvailableThemes[_currentTheme];

    private ThemeService()
    {
        var settings = new QuickTranslate.Core.Services.SettingsStore();
        var savedTheme = settings.Load().ColorTheme;
        
        if (!string.IsNullOrEmpty(savedTheme) && Enum.TryParse<AppTheme>(savedTheme, out var theme))
        {
            _currentTheme = theme;
        }
    }

    public void SetTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        ApplyTheme(theme);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyTheme(AppTheme theme)
    {
        var themeInfo = AvailableThemes[theme];
        
        ApplicationAccentColorManager.Apply(
            systemAccent: themeInfo.AccentColor,
            primaryAccent: themeInfo.PrimaryAccent,
            secondaryAccent: themeInfo.SecondaryAccent,
            tertiaryAccent: themeInfo.TertiaryAccent
        );
        
        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, updateAccent: false);
    }

    public void Initialize()
    {
        ApplyTheme(_currentTheme);
    }
}
