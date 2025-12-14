using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Desktop.Services;
using QuickTranslate.Desktop.Services.Interfaces;
using QuickTranslate.Desktop.ViewModels;
using Serilog;

namespace QuickTranslate.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly HotkeyService _hotkeyService;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsStore _settingsStore;
    private readonly ILogger _logger;
    private bool _isExiting;

    public MainWindow(MainViewModel viewModel, HotkeyService hotkeyService, IClipboardService clipboardService, ISettingsStore settingsStore)
    {
        InitializeComponent();
        
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        _clipboardService = clipboardService;
        _settingsStore = settingsStore;
        _logger = Log.ForContext<MainWindow>();
        
        DataContext = _viewModel;
        
        Loaded += MainWindow_Loaded;
        
        CreateTrayIcon();
    }
    
    private void CreateTrayIcon()
    {
        try
        {
            using var bitmap = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.FromArgb(0, 120, 212));
            using var font = new Font("Segoe UI", 16, System.Drawing.FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            g.DrawString("T", font, brush, 6, 2);
            
            var hIcon = bitmap.GetHicon();
            var icon = System.Drawing.Icon.FromHandle(hIcon);
            TrayIcon.Icon = icon;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to create tray icon");
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        _hotkeyService.Initialize(helper.Handle);
        
        RegisterHotkeysFromSettings();
        
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        
        _logger.Information("Main window loaded and hotkeys registered");
    }

    public void RegisterHotkeysFromSettings()
    {
        var settings = _settingsStore.Load();
        
        _hotkeyService.UnregisterAll();
        
        _hotkeyService.RegisterHotkey(HotkeyAction.TranslateSelection, 
            settings.TranslateSelectionHotkey.Modifiers, 
            settings.TranslateSelectionHotkey.Key);
        
        _hotkeyService.RegisterHotkey(HotkeyAction.ShowHide, 
            settings.ShowHideHotkey.Modifiers, 
            settings.ShowHideHotkey.Key);
        
        _logger.Information("Hotkeys registered from settings");
    }

    private async void OnHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        _logger.Information("Hotkey received: {Action}", e.Action);
        
        switch (e.Action)
        {
            case HotkeyAction.TranslateSelection:
                // Capture selected text using the foreground window captured at hotkey press time
                var selectedText = await _clipboardService.GetSelectedTextAsync(e.ForegroundWindow);
                if (!string.IsNullOrWhiteSpace(selectedText))
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        var popup = new TranslationPopup();
                        popup.SetLoading();
                        popup.ShowAtCursor();
                        
                        var result = await _viewModel.TranslateForPopupAsync(selectedText);
                        
                        if (result.Success && result.Translation != null)
                        {
                            popup.SetTranslation(result.Translation);
                        }
                        else
                        {
                            popup.SetError(result.Error ?? "Translation failed");
                        }
                    });
                }
                else
                {
                    _logger.Warning("No text selected for translation");
                }
                break;
                
            case HotkeyAction.ShowHide:
                Dispatcher.Invoke(() =>
                {
                    if (IsVisible && WindowState != WindowState.Minimized)
                    {
                        Hide();
                    }
                    else
                    {
                        ShowAndActivate();
                    }
                });
                break;
        }
    }

    private void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
            _logger.Information("Window hidden to tray");
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void TrayShow_Click(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
    }

    private void TraySettings_Click(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
        Settings_Click(sender, e);
    }

    private void TrayExit_Click(object sender, RoutedEventArgs e)
    {
        _isExiting = true;
        _hotkeyService.Dispose();
        TrayIcon.Dispose();
        Application.Current.Shutdown();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = App.GetService<SettingsWindow>();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
        
        _viewModel.LoadSettings();
        RegisterHotkeysFromSettings();
    }
}
