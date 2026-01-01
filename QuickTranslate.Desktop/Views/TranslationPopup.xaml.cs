using System.Runtime.InteropServices;
using System.Windows;
using QuickTranslate.Desktop.Services.Interfaces;
using QuickTranslate.Desktop.ViewModels;

namespace QuickTranslate.Desktop.Views;

public partial class TranslationPopup : Window
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private readonly TranslationPopupViewModel _viewModel;

    public TranslationPopupViewModel ViewModel => _viewModel;

    public TranslationPopup(IClipboardService clipboardService)
    {
        InitializeComponent();
        _viewModel = new TranslationPopupViewModel(clipboardService);
        DataContext = _viewModel;
    }

    public void SetTranslation(string text)
    {
        _viewModel.SetTranslation(text);
    }

    public void SetError(string error)
    {
        _viewModel.SetError(error);
    }

    public void SetLoading()
    {
        _viewModel.SetLoading();
    }

    public void ShowAtCursor()
    {
        GetCursorPos(out var cursorPos);

        Left = cursorPos.X;
        Top = cursorPos.Y + 20;

        Show();
        Activate();

        Dispatcher.BeginInvoke(new Action(() =>
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            if (Left + ActualWidth > screenWidth)
                Left = screenWidth - ActualWidth - 10;

            if (Top + ActualHeight > screenHeight)
                Top = cursorPos.Y - ActualHeight - 10;

            if (Left < 0) Left = 10;
            if (Top < 0) Top = 10;
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        try
        {
            if (IsLoaded)
            {
                Close();
            }
        }
        catch { }
    }

    private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
