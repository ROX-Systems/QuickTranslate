using System.Windows;
using QuickTranslate.Desktop.ViewModels;

namespace QuickTranslate.Desktop.Views;

public partial class SettingsWindow : Window
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
}
