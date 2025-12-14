using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using QuickTranslate.Desktop.Services;
using QuickTranslate.Desktop.Services.Interfaces;
using QuickTranslate.Desktop.ViewModels;
using Serilog;

namespace QuickTranslate.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly HotkeyService _hotkeyService;
    private readonly ILogger _logger;
    private bool _isExiting;

    public MainWindow(MainViewModel viewModel, HotkeyService hotkeyService)
    {
        InitializeComponent();
        
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
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
        
        _hotkeyService.RegisterHotkey(HotkeyAction.TranslateSelection, 
            HotkeyService.Modifiers.Control | HotkeyService.Modifiers.Shift, 
            HotkeyService.Keys.T);
        
        _hotkeyService.RegisterHotkey(HotkeyAction.TranslatePage, 
            HotkeyService.Modifiers.Control | HotkeyService.Modifiers.Shift, 
            HotkeyService.Keys.P);
        
        _hotkeyService.RegisterHotkey(HotkeyAction.ShowHide, 
            HotkeyService.Modifiers.Control | HotkeyService.Modifiers.Shift, 
            HotkeyService.Keys.O);
        
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        
        _logger.Information("Main window loaded and hotkeys registered");
    }

    private async void OnHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        _logger.Information("Hotkey received: {Action}", e.Action);
        
        switch (e.Action)
        {
            case HotkeyAction.TranslateSelection:
                await Dispatcher.InvokeAsync(async () =>
                {
                    ShowAndActivate();
                    await Task.Delay(100);
                    await _viewModel.TranslateSelectionCommand.ExecuteAsync(null);
                });
                break;
                
            case HotkeyAction.TranslatePage:
                await Dispatcher.InvokeAsync(async () =>
                {
                    await _viewModel.TranslatePageCommand.ExecuteAsync(null);
                    ShowAndActivate();
                });
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
    }
}
