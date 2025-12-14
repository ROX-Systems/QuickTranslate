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
    private readonly IBrowserService _browserService;
    private readonly IHtmlExtractor _htmlExtractor;
    private readonly ILogger _logger;

    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private string _sourceText = string.Empty;

    [ObservableProperty]
    private string _translatedText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _selectedTargetLanguage = "Russian";

    [ObservableProperty]
    private ObservableCollection<ProviderConfig> _providers = new();

    [ObservableProperty]
    private ProviderConfig? _selectedProvider;

    public string[] AvailableLanguages { get; } = { "Russian", "English", "German", "French", "Spanish", "Chinese", "Japanese", "Korean" };

    public MainViewModel(
        ITranslationService translationService,
        ISettingsStore settingsStore,
        IProviderClient providerClient,
        IClipboardService clipboardService,
        IBrowserService browserService,
        IHtmlExtractor htmlExtractor)
    {
        _translationService = translationService;
        _settingsStore = settingsStore;
        _providerClient = providerClient;
        _clipboardService = clipboardService;
        _browserService = browserService;
        _htmlExtractor = htmlExtractor;
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
            ShowError("No text selected. Please select some text and try again.");
            return;
        }

        SourceText = selectedText;
        await TranslateCurrentTextAsync();
    }

    public async Task TranslatePageFromWindowAsync(IntPtr browserWindow)
    {
        _logger.Information("TranslatePage triggered for window: {Hwnd}", browserWindow);

        if (!_browserService.IsBrowserActive(browserWindow))
        {
            ShowError("No browser detected. Please open a browser and navigate to a page.");
            return;
        }

        var url = await _browserService.GetCurrentUrlAsync(browserWindow);
        await TranslatePageContentAsync(url);
    }

    [RelayCommand]
    private async Task TranslatePageAsync()
    {
        _logger.Information("TranslatePage triggered");

        if (!_browserService.IsBrowserActive())
        {
            ShowError("No browser detected. Please open a browser and navigate to a page.");
            return;
        }

        var url = await _browserService.GetCurrentUrlAsync();
        await TranslatePageContentAsync(url);
    }

    private async Task TranslatePageContentAsync(string? url)
    {
        
        if (string.IsNullOrEmpty(url))
        {
            ShowError("Could not get URL from browser. Try using 'Translate Selection' instead.");
            return;
        }

        StatusMessage = "Fetching page content...";
        IsLoading = true;
        HasError = false;

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var pageText = await _htmlExtractor.ExtractTextFromUrlAsync(url, _cancellationTokenSource.Token);

            if (string.IsNullOrWhiteSpace(pageText))
            {
                ShowError("Could not extract text from page.");
                return;
            }

            const int maxLength = 15000;
            if (pageText.Length > maxLength)
            {
                pageText = pageText.Substring(0, maxLength) + "\n\n[Text truncated...]";
            }

            SourceText = pageText;
            await TranslateCurrentTextAsync();
        }
        catch (TaskCanceledException)
        {
            StatusMessage = "Cancelled";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error fetching page");
            ShowError($"Error fetching page: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(SourceText))
        {
            ShowError("Please enter text to translate.");
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
        StatusMessage = "Translating...";
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
                    ? $"Translated from {result.DetectedLanguage}"
                    : "Translation complete";
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Translation failed");
            }
        }
        catch (TaskCanceledException)
        {
            StatusMessage = "Cancelled";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Translation error");
            ShowError($"Error: {ex.Message}");
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
            StatusMessage = "Copied to clipboard";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CancelCurrentOperation();
        SourceText = string.Empty;
        TranslatedText = string.Empty;
        StatusMessage = "Ready";
        HasError = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelCurrentOperation();
        StatusMessage = "Cancelled";
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
}
