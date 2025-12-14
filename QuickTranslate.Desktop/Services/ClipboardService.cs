using System.Runtime.InteropServices;
using System.Windows;
using QuickTranslate.Desktop.Services.Interfaces;
using Serilog;

namespace QuickTranslate.Desktop.Services;

public class ClipboardService : IClipboardService
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const byte VK_CONTROL = 0x11;
    private const byte VK_C = 0x43;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private readonly ILogger _logger;

    public ClipboardService()
    {
        _logger = Log.ForContext<ClipboardService>();
    }

    public async Task<string?> GetSelectedTextAsync()
    {
        try
        {
            var foregroundWindow = GetForegroundWindow();
            
            IDataObject? previousClipboardData = null;
            string? previousText = null;
            bool hadText = false;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        hadText = true;
                        previousText = Clipboard.GetText();
                    }
                    else
                    {
                        previousClipboardData = Clipboard.GetDataObject();
                    }
                    Clipboard.Clear();
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to backup clipboard");
                }
            });

            SetForegroundWindow(foregroundWindow);
            await Task.Delay(50);

            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            await Task.Delay(50);
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            await Task.Delay(150);

            string? selectedText = null;
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        selectedText = Clipboard.GetText();
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
                    if (hadText && previousText != null)
                    {
                        Clipboard.SetText(previousText);
                    }
                    else if (previousClipboardData != null)
                    {
                        Clipboard.SetDataObject(previousClipboardData, true);
                    }
                    else
                    {
                        Clipboard.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to restore clipboard");
                }
            });

            if (string.IsNullOrWhiteSpace(selectedText))
            {
                _logger.Information("No text selected or clipboard empty");
                return null;
            }

            _logger.Information("Retrieved selected text: {Length} characters", selectedText.Length);
            return selectedText;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting selected text");
            return null;
        }
    }

    public void SetText(string text)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Clipboard.SetText(text);
            });
            _logger.Information("Text copied to clipboard: {Length} characters", text.Length);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to set clipboard text");
        }
    }

    public string? GetText()
    {
        try
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                if (Clipboard.ContainsText())
                {
                    return Clipboard.GetText();
                }
                return null;
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get clipboard text");
            return null;
        }
    }
}
