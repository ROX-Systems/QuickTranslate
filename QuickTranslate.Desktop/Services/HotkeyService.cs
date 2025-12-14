using System.Runtime.InteropServices;
using System.Windows.Interop;
using QuickTranslate.Desktop.Services.Interfaces;
using Serilog;

namespace QuickTranslate.Desktop.Services;

public class HotkeyService : IHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    public static class Modifiers
    {
        public const uint Alt = 0x0001;
        public const uint Control = 0x0002;
        public const uint Shift = 0x0004;
        public const uint Win = 0x0008;
        public const uint NoRepeat = 0x4000;
    }

    public static class Keys
    {
        public const uint T = 0x54;
        public const uint P = 0x50;
        public const uint O = 0x4F;
    }

    private readonly Dictionary<int, HotkeyAction> _registeredHotkeys = new();
    private readonly ILogger _logger;
    private IntPtr _windowHandle;
    private HwndSource? _hwndSource;
    private int _nextId = 1;
    private DateTime _lastHotkeyTime = DateTime.MinValue;
    private const int DebounceMs = 300;

    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    public HotkeyService()
    {
        _logger = Log.ForContext<HotkeyService>();
    }

    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        _hwndSource?.AddHook(WndProc);
        _logger.Information("HotkeyService initialized with window handle: {Handle}", windowHandle);
    }

    public void RegisterHotkey(HotkeyAction action, uint modifiers, uint key)
    {
        var id = _nextId++;
        
        if (RegisterHotKey(_windowHandle, id, modifiers | Modifiers.NoRepeat, key))
        {
            _registeredHotkeys[id] = action;
            _logger.Information("Registered hotkey {Action} with id {Id}", action, id);
        }
        else
        {
            var error = Marshal.GetLastWin32Error();
            _logger.Error("Failed to register hotkey {Action}. Error: {Error}", action, error);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _registeredHotkeys.Keys)
        {
            UnregisterHotKey(_windowHandle, id);
            _logger.Information("Unregistered hotkey with id {Id}", id);
        }
        _registeredHotkeys.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            
            if (_registeredHotkeys.TryGetValue(id, out var action))
            {
                var now = DateTime.Now;
                if ((now - _lastHotkeyTime).TotalMilliseconds >= DebounceMs)
                {
                    _lastHotkeyTime = now;
                    var foregroundWindow = GetForegroundWindow();
                    _logger.Information("Hotkey pressed: {Action}, ForegroundWindow: {Hwnd}", action, foregroundWindow);
                    HotkeyPressed?.Invoke(this, new HotkeyEventArgs(action, foregroundWindow));
                }
                else
                {
                    _logger.Debug("Hotkey debounced: {Action}", action);
                }
                handled = true;
            }
        }
        
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
    }
}
