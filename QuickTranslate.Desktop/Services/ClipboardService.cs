using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using QuickTranslate.Desktop.Services.Interfaces;
using Serilog;

namespace QuickTranslate.Desktop.Services;

public class ClipboardService : IClipboardService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);


    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);


    private readonly ILogger _logger;

    public ClipboardService()
    {
        _logger = Log.ForContext<ClipboardService>();
    }

    public async Task<string?> GetSelectedTextAsync(IntPtr? targetWindow = null)
    {
        try
        {
            var foregroundWindow = targetWindow ?? GetForegroundWindow();
            _logger.Information("GetSelectedTextAsync targeting window: {Hwnd}", foregroundWindow);
            
            // First try UI Automation to get selected text directly
            string? uiaText = null;
            try
            {
                var focusedElement = AutomationElement.FocusedElement;
                if (focusedElement != null)
                {
                    _logger.Information("Focused element: {Name}, {ControlType}", 
                        focusedElement.Current.Name, focusedElement.Current.ControlType.ProgrammaticName);
                    
                    if (focusedElement.TryGetCurrentPattern(TextPattern.Pattern, out object? pattern))
                    {
                        var textPattern = (TextPattern)pattern;
                        var selection = textPattern.GetSelection();
                        if (selection.Length > 0)
                        {
                            uiaText = selection[0].GetText(-1);
                            _logger.Information("Got text via UI Automation: {Length} chars", uiaText?.Length ?? 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "UI Automation failed, falling back to clipboard");
            }

            if (!string.IsNullOrWhiteSpace(uiaText))
            {
                return uiaText;
            }

            // Fallback to clipboard method
            IDataObject? previousClipboardData = null;
            string? previousText = null;
            bool hadText = false;
            bool backupSuccess = false;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        hadText = true;
                        previousText = Clipboard.GetText();
                        _logger.Debug("Backed up text from clipboard: {Length} chars", previousText?.Length ?? 0);
                    }
                    else
                    {
                        previousClipboardData = Clipboard.GetDataObject();
                        _logger.Debug("Backed up non-text data from clipboard");
                    }
                    backupSuccess = true;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to backup clipboard - clipboard may be corrupted");
                    backupSuccess = false;
                }
            });

            // Attach to target window's thread for reliable focus
            var targetThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            var currentThreadId = GetCurrentThreadId();
            bool attached = false;
            
            if (targetThreadId != currentThreadId)
            {
                attached = AttachThreadInput(currentThreadId, targetThreadId, true);
                _logger.Information("Thread attached: {Attached}", attached);
            }

            try
            {
                var setFgResult = SetForegroundWindow(foregroundWindow);
                _logger.Information("SetForegroundWindow result: {Result}", setFgResult);
                await Task.Delay(100);
                
                var actualForeground = GetForegroundWindow();
                _logger.Information("Actual foreground after SetForegroundWindow: {Hwnd} (target was {Target})", 
                    actualForeground, foregroundWindow);

                // Release any held modifier keys first (hotkey may have Ctrl+Shift held)
                keybd_event(0x11, 0, 0x0002, UIntPtr.Zero); // Ctrl up
                keybd_event(0x10, 0, 0x0002, UIntPtr.Zero); // Shift up
                keybd_event(0x12, 0, 0x0002, UIntPtr.Zero); // Alt up
                await Task.Delay(50);

                // Send Ctrl+C using keybd_event
                keybd_event(0x11, 0, 0, UIntPtr.Zero); // Ctrl down
                await Task.Delay(30);
                keybd_event(0x43, 0, 0, UIntPtr.Zero); // C down
                await Task.Delay(30);
                keybd_event(0x43, 0, 0x0002, UIntPtr.Zero); // C up
                await Task.Delay(30);
                keybd_event(0x11, 0, 0x0002, UIntPtr.Zero); // Ctrl up
                
                _logger.Information("Sent Ctrl+C via keybd_event");

                await Task.Delay(250);
            }
            finally
            {
                if (attached)
                {
                    AttachThreadInput(currentThreadId, targetThreadId, false);
                }
            }

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
                if (!backupSuccess)
                {
                    _logger.Warning("Skipping clipboard restore due to backup failure");
                    return;
                }

                try
                {
                    if (hadText && previousText != null)
                    {
                        Clipboard.SetText(previousText);
                        _logger.Debug("Restored text to clipboard: {Length} chars", previousText.Length);
                    }
                    else if (previousClipboardData != null)
                    {
                        Clipboard.SetDataObject(previousClipboardData, true);
                        _logger.Debug("Restored non-text data to clipboard");
                    }
                    else
                    {
                        Clipboard.Clear();
                        _logger.Debug("Cleared clipboard (was empty)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to restore clipboard - original clipboard content may be lost");
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

    public void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            _logger.Warning("Attempted to copy empty text to clipboard");
            return;
        }

        SetText(text);
    }
}
