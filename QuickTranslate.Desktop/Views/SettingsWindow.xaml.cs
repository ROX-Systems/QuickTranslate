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
        InitializeComponent();
        
        _viewModel = viewModel;
        DataContext = _viewModel;
        
        _viewModel.SettingsSaved += (s, e) => Close();
        _viewModel.CloseRequested += (s, e) => Close();
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
        var button = (Wpf.Ui.Controls.Button)sender;
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
}
