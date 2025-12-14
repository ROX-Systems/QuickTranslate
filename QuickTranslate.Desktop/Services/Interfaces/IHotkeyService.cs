namespace QuickTranslate.Desktop.Services.Interfaces;

public interface IHotkeyService : IDisposable
{
    event EventHandler<HotkeyEventArgs>? HotkeyPressed;
    void RegisterHotkey(HotkeyAction action, uint modifiers, uint key);
    void UnregisterAll();
}

public enum HotkeyAction
{
    TranslateSelection,
    ShowHide
}

public class HotkeyEventArgs : EventArgs
{
    public HotkeyAction Action { get; }
    public IntPtr ForegroundWindow { get; }
    
    public HotkeyEventArgs(HotkeyAction action, IntPtr foregroundWindow)
    {
        Action = action;
        ForegroundWindow = foregroundWindow;
    }
}
