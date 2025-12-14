using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using QuickTranslate.Desktop.Services.Interfaces;
using Serilog;

namespace QuickTranslate.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITranslationService _translationService;
    private readonly ISettingsStore _settingsStore;
    private readonly IProviderClient _providerClient;
    private readonly IClipboardService _clipboardService;
    private readonly ILogger _logger;

    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private string _sourceText = string.Empty;

    [ObservableProperty]
    private string _translatedText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Готов";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _selectedTargetLanguage = "Русский";

    [ObservableProperty]
    private ObservableCollection<ProviderConfig> _providers = new();

    [ObservableProperty]
    private ProviderConfig? _selectedProvider;

    public string[] AvailableLanguages { get; } = { "Русский", "Английский", "Немецкий", "Французский", "Испанский", "Китайский", "Японский", "Корейский" };

    public int SourceCharacterCount => SourceText?.Length ?? 0;
    public int TranslatedCharacterCount => TranslatedText?.Length ?? 0;

    public MainViewModel(
        ITranslationService translationService,
        ISettingsStore settingsStore,
        IProviderClient providerClient,
        IClipboardService clipboardService)
    {
        _translationService = translationService;
        _settingsStore = settingsStore;
        _providerClient = providerClient;
        _clipboardService = clipboardService;
        _logger = Log.ForContext<MainViewModel>();

        LoadSettings();
    }

    public void LoadSettings()
    {
        var settings = _settingsStore.Load();
        SelectedTargetLanguage = settings.TargetLanguage;
        
        Providers = new ObservableCollection<ProviderConfig>(settings.Providers);
        
        var activeProvider = settings.GetActiveProvider();
        if (activeProvider != null)
        {
            SelectedProvider = Providers.FirstOrDefault(p => p.Id == activeProvider.Id) ?? Providers.FirstOrDefault();
        }
        else
        {
            SelectedProvider = Providers.FirstOrDefault();
        }
    }

    partial void OnSelectedProviderChanged(ProviderConfig? value)
    {
        if (value != null)
        {
            _providerClient.UpdateProvider(value);
            
            var settings = _settingsStore.Load();
            settings.ActiveProviderId = value.Id;
            _settingsStore.Save(settings);
            
            StatusMessage = $"Provider: {value.Name}";
            _logger.Information("Active provider changed to: {Name}", value.Name);
        }
    }

    [RelayCommand]
    private async Task TranslateSelectionAsync()
    {
        _logger.Information("TranslateSelection triggered");
        
        var selectedText = await _clipboardService.GetSelectedTextAsync();
        
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            ShowError("Выделенный текст не найден. Выделите текст и попробуйте снова.");
            return;
        }

        SourceText = selectedText;
        await TranslateCurrentTextAsync();
    }

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(SourceText))
        {
            ShowError("Введите текст для перевода.");
            return;
        }

        await TranslateCurrentTextAsync();
    }

    private async Task TranslateCurrentTextAsync()
    {
        if (string.IsNullOrWhiteSpace(SourceText))
            return;

        CancelCurrentOperation();
        
        IsLoading = true;
        HasError = false;
        StatusMessage = "Перевод...";
        TranslatedText = string.Empty;

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            var request = new TranslationRequest
            {
                SourceText = SourceText,
                TargetLanguage = SelectedTargetLanguage
            };

            var result = await _translationService.TranslateAsync(request, _cancellationTokenSource.Token);

            if (result.Success)
            {
                TranslatedText = result.TranslatedText;
                StatusMessage = result.DetectedLanguage != null 
                    ? $"Переведено с {result.DetectedLanguage}"
                    : "Перевод завершён";
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Ошибка перевода");
            }
        }
        catch (TaskCanceledException)
        {
            StatusMessage = "Отменено";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Translation error");
            ShowError($"Ошибка: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CopyTranslation()
    {
        if (!string.IsNullOrEmpty(TranslatedText))
        {
            _clipboardService.SetText(TranslatedText);
            StatusMessage = "Скопировано в буфер обмена";
        }
    }

    public async Task<(bool Success, string? Translation, string? Error)> TranslateForPopupAsync(string text)
    {
        try
        {
            var settings = _settingsStore.Load();
            var request = new TranslationRequest
            {
                SourceText = text,
                TargetLanguage = settings.TargetLanguage
            };

            var result = await _translationService.TranslateAsync(request, CancellationToken.None);

            if (result.Success)
            {
                return (true, result.TranslatedText, null);
            }
            else
            {
                return (false, null, result.ErrorMessage ?? "Translation failed");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Popup translation error");
            return (false, null, ex.Message);
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CancelCurrentOperation();
        SourceText = string.Empty;
        TranslatedText = string.Empty;
        StatusMessage = "Готов";
        HasError = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelCurrentOperation();
        StatusMessage = "Отменено";
        IsLoading = false;
    }

    private void CancelCurrentOperation()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private void ShowError(string message)
    {
        HasError = true;
        StatusMessage = message;
        IsLoading = false;
        _logger.Warning("Error shown to user: {Message}", message);
    }

    public async Task TranslateTextAsync(string text)
    {
        SourceText = text;
        await TranslateCurrentTextAsync();
    }

    public void ShowNoTextSelectedError()
    {
        ShowError("No text selected. Please select some text and try again.");
    }

    partial void OnSelectedTargetLanguageChanged(string value)
    {
        var settings = _settingsStore.Load();
        settings.TargetLanguage = value;
        _settingsStore.Save(settings);
    }

    partial void OnSourceTextChanged(string value)
    {
        OnPropertyChanged(nameof(SourceCharacterCount));
    }

    partial void OnTranslatedTextChanged(string value)
    {
        OnPropertyChanged(nameof(TranslatedCharacterCount));
    }

    [RelayCommand]
    private void SwapTexts()
    {
        if (!string.IsNullOrEmpty(TranslatedText))
        {
            var temp = SourceText;
            SourceText = TranslatedText;
            TranslatedText = temp;
            StatusMessage = "Тексты поменяны местами";
        }
    }

    [RelayCommand]
    private void Paste()
    {
        var text = _clipboardService.GetText();
        if (!string.IsNullOrEmpty(text))
        {
            SourceText = text;
            StatusMessage = "Вставлено из буфера обмена";
        }
    }
}
