using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using QuickTranslate.Desktop.Services.Interfaces;
using Serilog;

namespace QuickTranslate.Desktop.Services;

public class BrowserService : IBrowserService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const byte VK_CONTROL = 0x11;
    private const byte VK_L = 0x4C;
    private const byte VK_C = 0x43;
    private const byte VK_ESCAPE = 0x1B;
    private const byte VK_F6 = 0x75;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private readonly string[] _browserProcessNames = 
    {
        "chrome", "msedge", "firefox", "opera", "brave", "vivaldi", "chromium", "browser", "arc", "yandex"
    };

    private readonly ILogger _logger;

    public BrowserService()
    {
        _logger = Log.ForContext<BrowserService>();
    }

    public bool IsBrowserActive(IntPtr? targetWindow = null)
    {
        var browserName = GetActiveBrowserName(targetWindow);
        return browserName != null;
    }

    public string? GetActiveBrowserName(IntPtr? targetWindow = null)
    {
        try
        {
            var hwnd = targetWindow ?? GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out var processId);
            
            var process = Process.GetProcessById((int)processId);
            var processName = process.ProcessName.ToLowerInvariant();
            
            _logger.Information("Checking window {Hwnd}, process: {ProcessName} (PID: {ProcessId})", 
                hwnd, processName, processId);

            foreach (var browser in _browserProcessNames)
            {
                if (processName.Contains(browser))
                {
                    _logger.Information("Active browser detected: {Browser}", processName);
                    return processName;
                }
            }
            
            _logger.Information("Window {Hwnd} is not a browser (process: {ProcessName})", hwnd, processName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error detecting active browser");
            return null;
        }
    }

    public async Task<string?> GetCurrentUrlAsync(IntPtr? targetWindow = null)
    {
        try
        {
            var foregroundWindow = targetWindow ?? GetForegroundWindow();
            
            if (!IsBrowserActive(foregroundWindow))
            {
                _logger.Warning("No browser is currently active");
                return null;
            }
            
            string? previousClipboard = null;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        previousClipboard = Clipboard.GetText();
                    }
                    Clipboard.Clear();
                }
                catch { }
            });

            SetForegroundWindow(foregroundWindow);
            await Task.Delay(100);

            // Release any held modifier keys first (hotkey may have Ctrl+Shift held)
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Shift
            keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Alt
            await Task.Delay(50);

            // Try F6 first (works in most browsers including Arc), then Ctrl+L as fallback
            _logger.Information("Sending F6 to select address bar");
            keybd_event(VK_F6, 0, 0, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_F6, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            
            await Task.Delay(100);
            
            // Also try Ctrl+L as some browsers prefer it
            _logger.Information("Sending Ctrl+L to select address bar");
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_L, 0, 0, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_L, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            await Task.Delay(150);

            // Ctrl+C to copy URL
            _logger.Information("Sending Ctrl+C to copy URL");
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            await Task.Delay(150);

            // Escape to deselect address bar
            keybd_event(VK_ESCAPE, 0, 0, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            await Task.Delay(100);

            string? url = null;
            string? clipboardContent = null;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        clipboardContent = Clipboard.GetText()?.Trim();
                        _logger.Information("Clipboard content after Ctrl+C: {Content}", clipboardContent);
                        if (Uri.TryCreate(clipboardContent, UriKind.Absolute, out var uri) &&
                            (uri.Scheme == "http" || uri.Scheme == "https"))
                        {
                            url = clipboardContent;
                        }
                        else
                        {
                            _logger.Warning("Clipboard content is not a valid URL");
                        }
                    }
                    else
                    {
                        _logger.Warning("Clipboard is empty after Ctrl+C");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to read clipboard");
                }
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (previousClipboard != null)
                    {
                        Clipboard.SetText(previousClipboard);
                    }
                    else
                    {
                        Clipboard.Clear();
                    }
                }
                catch { }
            });

            if (url != null)
            {
                _logger.Information("Retrieved URL: {Url}", url);
            }
            else
            {
                _logger.Warning("Failed to retrieve URL from browser");
            }

            return url;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting current URL");
            return null;
        }
    }
}
