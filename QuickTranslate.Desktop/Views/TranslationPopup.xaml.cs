using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using QuickTranslate.Desktop.Services.Interfaces;

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

    private readonly IClipboardService _clipboardService;

    public string TranslatedText { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsLoading { get; set; }
    public bool HasError { get; set; }

    public TranslationPopup(IClipboardService clipboardService)
    {
        InitializeComponent();
        _clipboardService = clipboardService;
        DataContext = this;
    }

    public void SetTranslation(string text)
    {
        TranslatedText = text;
        TranslationText.Text = text;
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorBorder.Visibility = Visibility.Collapsed;
        TranslationScrollViewer.Visibility = Visibility.Visible;
        CharacterCountText.Text = $"{text.Length} {Services.LocalizationService.Instance["Characters"]}";
    }

    public void SetError(string error)
    {
        ErrorText.Text = error;
        LoadingPanel.Visibility = Visibility.Collapsed;
        TranslationScrollViewer.Visibility = Visibility.Collapsed;
        ErrorBorder.Visibility = Visibility.Visible;
        CharacterCountText.Text = string.Empty;
    }

    public void SetLoading()
    {
        LoadingPanel.Visibility = Visibility.Visible;
        TranslationScrollViewer.Visibility = Visibility.Collapsed;
        ErrorBorder.Visibility = Visibility.Collapsed;
        CharacterCountText.Text = string.Empty;
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

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        _clipboardService.CopyToClipboard(TranslatedText);
    }

    private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
