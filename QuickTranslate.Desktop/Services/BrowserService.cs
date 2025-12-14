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
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private readonly string[] _browserProcessNames = 
    {
        "chrome", "msedge", "firefox", "opera", "brave", "vivaldi", "chromium"
    };

    private readonly ILogger _logger;

    public BrowserService()
    {
        _logger = Log.ForContext<BrowserService>();
    }

    public bool IsBrowserActive()
    {
        var browserName = GetActiveBrowserName();
        return browserName != null;
    }

    public string? GetActiveBrowserName()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out var processId);
            
            var process = Process.GetProcessById((int)processId);
            var processName = process.ProcessName.ToLowerInvariant();

            foreach (var browser in _browserProcessNames)
            {
                if (processName.Contains(browser))
                {
                    _logger.Information("Active browser detected: {Browser}", processName);
                    return processName;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error detecting active browser");
            return null;
        }
    }

    public async Task<string?> GetCurrentUrlAsync()
    {
        try
        {
            if (!IsBrowserActive())
            {
                _logger.Warning("No browser is currently active");
                return null;
            }

            var foregroundWindow = GetForegroundWindow();
            
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
            await Task.Delay(50);

            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_L, 0, 0, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_L, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            await Task.Delay(100);

            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            await Task.Delay(100);

            keybd_event(VK_ESCAPE, 0, 0, UIntPtr.Zero);
            await Task.Delay(30);
            keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            await Task.Delay(50);

            string? url = null;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        var text = Clipboard.GetText()?.Trim();
                        if (Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
                            (uri.Scheme == "http" || uri.Scheme == "https"))
                        {
                            url = text;
                        }
                    }
                }
                catch { }
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
