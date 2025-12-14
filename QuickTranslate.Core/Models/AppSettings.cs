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
    public List<ProviderConfig> Providers { get; set; } = new();
    public string? ActiveProviderId { get; set; }
    public string TargetLanguage { get; set; } = "Russian";
    
    private HotkeyConfig _translateSelectionHotkey = new(0x0006, 0x54); // Ctrl+Shift+T
    private HotkeyConfig _showHideHotkey = new(0x0006, 0x4F); // Ctrl+Shift+O

    public HotkeyConfig TranslateSelectionHotkey 
    { 
        get => _translateSelectionHotkey;
        set => _translateSelectionHotkey = IsValidHotkey(value) ? value : new(0x0006, 0x54);
    }
    
    public HotkeyConfig ShowHideHotkey 
    { 
        get => _showHideHotkey;
        set => _showHideHotkey = IsValidHotkey(value) ? value : new(0x0006, 0x4F);
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
