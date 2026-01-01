using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTranslate.Desktop.Services.Interfaces;

namespace QuickTranslate.Desktop.ViewModels;

public partial class TranslationPopupViewModel : ObservableObject
{
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    private string _translatedText = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _characterCountText = string.Empty;

    public TranslationPopupViewModel(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
    }

    public void SetTranslation(string text)
    {
        TranslatedText = text;
        ErrorMessage = string.Empty;
        IsLoading = false;
        HasError = false;
        CharacterCountText = $"{text.Length} {Services.LocalizationService.Instance["Characters"]}";
    }

    public void SetError(string error)
    {
        ErrorMessage = error;
        TranslatedText = string.Empty;
        IsLoading = false;
        HasError = true;
        CharacterCountText = string.Empty;
    }

    public void SetLoading()
    {
        IsLoading = true;
        TranslatedText = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
        CharacterCountText = string.Empty;
    }

    [RelayCommand]
    private void Close()
    {
    }

    [RelayCommand]
    private void Copy()
    {
        _clipboardService.CopyToClipboard(TranslatedText);
    }
}
