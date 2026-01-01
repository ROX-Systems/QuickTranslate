namespace QuickTranslate.Core.Models;

public class HotkeyConfig
{
    public uint Modifiers { get; set; }
    public uint Key { get; set; }
    
    public HotkeyConfig() { }
    
    public HotkeyConfig(uint modifiers, uint key)
    {
        Modifiers = modifiers;
        Key = key;
    }
}

public class AppSettings
{
    // Hotkey modifier constants
    public const uint ModifierCtrlShift = 0x0006;

    // Virtual key codes
    public const uint KeyT = 0x54;
    public const uint KeyO = 0x4F;

    public List<ProviderConfig> Providers { get; set; } = new();
    public string? ActiveProviderId { get; set; }
    public string TargetLanguage { get; set; } = "Russian";
    public string? InterfaceLanguage { get; set; } = "ru";
    public string? ColorTheme { get; set; } = "OceanBlue";
    public string ActiveProfileId { get; set; } = "general";
    public bool UseAutoProfileDetection { get; set; } = false;
    public bool TtsEnabled { get; set; } = true;
    public bool AutoSpeakAfterTranslate { get; set; } = false;
    public string? TtsEndpoint { get; set; } = "https://tts.rox-net.ru";

    private HotkeyConfig _translateSelectionHotkey = new(ModifierCtrlShift, KeyT);
    private HotkeyConfig _showHideHotkey = new(ModifierCtrlShift, KeyO);

    public HotkeyConfig TranslateSelectionHotkey
    {
        get => _translateSelectionHotkey;
        set => _translateSelectionHotkey = IsValidHotkey(value) ? value : new(ModifierCtrlShift, KeyT);
    }

    public HotkeyConfig ShowHideHotkey
    {
        get => _showHideHotkey;
        set => _showHideHotkey = IsValidHotkey(value) ? value : new(ModifierCtrlShift, KeyO);
    }

    private static bool IsValidHotkey(HotkeyConfig? hotkey) => 
        hotkey != null && hotkey.Key != 0 && hotkey.Modifiers != 0;

    public ProviderConfig? GetActiveProvider()
    {
        if (string.IsNullOrEmpty(ActiveProviderId))
            return Providers.FirstOrDefault();
        
        return Providers.FirstOrDefault(p => p.Id == ActiveProviderId) 
               ?? Providers.FirstOrDefault();
    }

    public void SetActiveProvider(string providerId)
    {
        if (Providers.Any(p => p.Id == providerId))
        {
            ActiveProviderId = providerId;
        }
    }
}
