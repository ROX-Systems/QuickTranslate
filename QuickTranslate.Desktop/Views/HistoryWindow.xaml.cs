using System.Windows;
using QuickTranslate.Core.Models;
using QuickTranslate.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace QuickTranslate.Desktop.Views;

public partial class HistoryWindow : FluentWindow
{
    private readonly HistoryViewModel _viewModel;

    public TranslationHistoryItem? SelectedItem { get; private set; }

    public HistoryWindow(HistoryViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        
        InitializeComponent();
        
        _viewModel.ItemSelected += OnItemSelected;
    }

    private void OnItemSelected(object? sender, TranslationHistoryItem item)
    {
        SelectedItem = item;
        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.ItemSelected -= OnItemSelected;
        base.OnClosed(e);
    }
}
