using System.Windows;
using System.Windows.Input;

namespace QuickTranslate.Desktop.Views;

public partial class TranslationPopup : Window
{
    public string TranslatedText { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsLoading { get; set; }
    public bool HasError { get; set; }

    public TranslationPopup()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void SetTranslation(string text)
    {
        TranslatedText = text;
        TranslationText.Text = text;
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorText.Visibility = Visibility.Collapsed;
        TranslationScrollViewer.Visibility = Visibility.Visible;
    }

    public void SetError(string error)
    {
        ErrorText.Text = error;
        LoadingPanel.Visibility = Visibility.Collapsed;
        TranslationScrollViewer.Visibility = Visibility.Collapsed;
        ErrorText.Visibility = Visibility.Visible;
    }

    public void SetLoading()
    {
        LoadingPanel.Visibility = Visibility.Visible;
        TranslationScrollViewer.Visibility = Visibility.Collapsed;
        ErrorText.Visibility = Visibility.Collapsed;
    }

    public void ShowAtCursor()
    {
        var cursorPos = System.Windows.Forms.Cursor.Position;
        
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
        if (!string.IsNullOrEmpty(TranslatedText))
        {
            Clipboard.SetText(TranslatedText);
        }
    }
}
