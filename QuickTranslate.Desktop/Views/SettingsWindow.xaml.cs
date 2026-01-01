using System.Windows;
using System.Windows.Input;
using QuickTranslate.Desktop.ViewModels;
using Wpf.Ui.Controls;
using TextBox = System.Windows.Controls.TextBox;

namespace QuickTranslate.Desktop.Views;

public partial class SettingsWindow : FluentWindow
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("SettingsWindow: Starting initialization");

            InitializeComponent();

            System.Diagnostics.Debug.WriteLine("SettingsWindow: InitializeComponent completed");

            _viewModel = viewModel;
            DataContext = _viewModel;

            System.Diagnostics.Debug.WriteLine("SettingsWindow: ViewModel assigned");

            _viewModel.SettingsSaved += (s, e) => Close();
            _viewModel.CloseRequested += (s, e) => Close();

            System.Diagnostics.Debug.WriteLine("SettingsWindow: Event handlers assigned");

            // Initialize API key box
            ApiKeyBox.Password = _viewModel.ApiKey ?? string.Empty;

            System.Diagnostics.Debug.WriteLine("SettingsWindow: API key box initialized");

            // Update API key box when provider changes
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.ApiKey))
                {
                    ApiKeyBox.Password = _viewModel.ApiKey ?? string.Empty;
                }
                else if (e.PropertyName == nameof(_viewModel.SelectedProvider))
                {
                    // Update password box when provider selection changes
                    ApiKeyBox.Password = _viewModel.ApiKey ?? string.Empty;
                }
            };

            System.Diagnostics.Debug.WriteLine("SettingsWindow: Initialization completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SettingsWindow: ERROR - {ex.Message}\n{ex.StackTrace}");

            System.Windows.MessageBox.Show(
                $"Failed to initialize settings window:\n\n{ex.Message}\n\nDetails:\n{ex}",
                "Settings Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);

            throw;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        
        var textBox = (TextBox)sender;
        var hotkeyName = textBox.Tag?.ToString();
        
        if (e.Key == Key.Escape)
        {
            Keyboard.ClearFocus();
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None)
        {
            return;
        }

        uint mod = 0;
        if ((modifiers & ModifierKeys.Alt) != 0) mod |= 0x0001;
        if ((modifiers & ModifierKeys.Control) != 0) mod |= 0x0002;
        if ((modifiers & ModifierKeys.Shift) != 0) mod |= 0x0004;
        if ((modifiers & ModifierKeys.Windows) != 0) mod |= 0x0008;

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        if (hotkeyName == "TranslateSelection")
        {
            _viewModel.SetTranslateSelectionHotkey(mod, vk);
        }
        else if (hotkeyName == "ShowHide")
        {
            _viewModel.SetShowHideHotkey(mod, vk);
        }

        Keyboard.ClearFocus();
    }

    private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        textBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3a, 0x3a, 0x3a));
    }

    private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        textBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2d, 0x2d, 0x2d));
    }

    private void ResetHotkey_Click(object sender, RoutedEventArgs e)
    {
        var button = (System.Windows.Controls.Button)sender;
        var hotkeyName = button.Tag?.ToString();

        if (hotkeyName == "TranslateSelection")
        {
            _viewModel.SetTranslateSelectionHotkey(0x0006, 0x54); // Ctrl+Shift+T
        }
        else if (hotkeyName == "ShowHide")
        {
            _viewModel.SetShowHideHotkey(0x0006, 0x4F); // Ctrl+Shift+O
        }
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.PasswordBox passwordBox)
        {
            _viewModel.ApiKey = passwordBox.Password;
        }
    }
}
