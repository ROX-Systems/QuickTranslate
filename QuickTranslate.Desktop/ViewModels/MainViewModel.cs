using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using QuickTranslate.Core.Services;
using QuickTranslate.Desktop.Services;
using QuickTranslate.Desktop.Services.Interfaces;
using Serilog;

namespace QuickTranslate.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITranslationService _translationService;
    private readonly ISettingsStore _settingsStore;
    private readonly IProviderClient _providerClient;
    private readonly IClipboardService _clipboardService;
    private readonly ITtsService _ttsService;
    private readonly IAudioPlayerService _audioPlayerService;
    private readonly ITranslationHistoryService _historyService;
    private readonly ILogger _logger;

    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private string _sourceText = string.Empty;

    [ObservableProperty]
    private string _translatedText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = LocalizationService.Instance["Ready"];

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _selectedTargetLanguage = "Russian";

    [ObservableProperty]
    private ObservableCollection<ProviderConfig> _providers = new();

    [ObservableProperty]
    private ProviderConfig? _selectedProvider;

    [ObservableProperty]
    private ObservableCollection<TranslationProfile> _translationProfiles = new();

    [ObservableProperty]
    private TranslationProfile? _selectedProfile;

    [ObservableProperty]
    private bool _useAutoProfileDetection;

    [ObservableProperty]
    private bool _isSpeaking;

    [ObservableProperty]
    private bool _canSpeak;

    public string[] AvailableLanguages { get; } = { "Russian", "English", "German", "French", "Spanish", "Chinese", "Japanese", "Korean" };

    public int SourceCharacterCount => SourceText?.Length ?? 0;
    public int TranslatedCharacterCount => TranslatedText?.Length ?? 0;
    
    public string SourceCharacterCountText => $"{SourceCharacterCount} {LocalizationService.Instance["Characters"]}";
    public string TranslatedCharacterCountText => $"{TranslatedCharacterCount} {LocalizationService.Instance["Characters"]}";

    public MainViewModel(
        ITranslationService translationService,
        ISettingsStore settingsStore,
        IProviderClient providerClient,
        IClipboardService clipboardService,
        ITtsService ttsService,
        IAudioPlayerService audioPlayerService,
        ITranslationHistoryService historyService)
    {
        _translationService = translationService;
        _settingsStore = settingsStore;
        _providerClient = providerClient;
        _clipboardService = clipboardService;
        _ttsService = ttsService;
        _audioPlayerService = audioPlayerService;
        _historyService = historyService;
        _logger = Log.ForContext<MainViewModel>();

        _audioPlayerService.PlaybackFinished += OnPlaybackFinished;

        LoadSettings();
    }

    private void OnPlaybackFinished(object? sender, EventArgs e)
    {
        IsSpeaking = false;
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
        
        TranslationProfiles = new ObservableCollection<TranslationProfile>(TranslationProfile.GetBuiltInProfiles());
        SelectedProfile = TranslationProfiles.FirstOrDefault(p => p.Id == settings.ActiveProfileId) 
                          ?? TranslationProfiles.FirstOrDefault();
        UseAutoProfileDetection = settings.UseAutoProfileDetection;
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
            ShowError(LocalizationService.Instance["NoTextSelected"]);
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
            ShowError(LocalizationService.Instance["EnterTextToTranslate"]);
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
        StatusMessage = LocalizationService.Instance["Translating"];
        TranslatedText = string.Empty;

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            var request = new TranslationRequest
            {
                SourceText = SourceText,
                TargetLanguage = SelectedTargetLanguage,
                Profile = UseAutoProfileDetection ? null : SelectedProfile,
                UseAutoDetection = UseAutoProfileDetection
            };

            var result = await _translationService.TranslateAsync(request, _cancellationTokenSource.Token);

            if (result.Success)
            {
                TranslatedText = result.TranslatedText;
                StatusMessage = result.DetectedLanguage != null 
                    ? string.Format(LocalizationService.Instance["TranslatedFrom"], result.DetectedLanguage)
                    : LocalizationService.Instance["TranslationComplete"];
                
                SaveToHistory(result.TranslatedText, result.DetectedLanguage);
            }
            else
            {
                ShowError(result.ErrorMessage ?? LocalizationService.Instance["TranslationFailed"]);
            }
        }
        catch (TaskCanceledException)
        {
            StatusMessage = LocalizationService.Instance["Cancelled"];
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Translation error");
            ShowError(string.Format(LocalizationService.Instance["Error"], ex.Message));
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
            StatusMessage = LocalizationService.Instance["CopiedToClipboard"];
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SpeakTranslationAsync()
    {
        if (string.IsNullOrWhiteSpace(TranslatedText))
            return;

        if (IsSpeaking)
        {
            _audioPlayerService.Stop();
            IsSpeaking = false;
            StatusMessage = LocalizationService.Instance["Ready"];
            return;
        }

        var langCode = LanguageNormalizer.Normalize(SelectedTargetLanguage);
        
        if (!_ttsService.IsLanguageSupported(langCode))
        {
            StatusMessage = LocalizationService.Instance["TtsLanguageNotSupported"];
            return;
        }

        try
        {
            IsSpeaking = true;
            StatusMessage = LocalizationService.Instance["Speaking"];
            
            var audioData = await _ttsService.SynthesizeAsync(TranslatedText, langCode);
            
            if (audioData != null && audioData.Length > 0)
            {
                await _audioPlayerService.PlayAsync(audioData);
            }
            else
            {
                StatusMessage = LocalizationService.Instance["TtsFailed"];
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "TTS error");
            StatusMessage = LocalizationService.Instance["TtsFailed"];
        }
        finally
        {
            IsSpeaking = false;
        }
    }

    public async Task<(bool Success, string? Translation, string? Error)> TranslateForPopupAsync(string text)
    {
        try
        {
            var settings = _settingsStore.Load();
            var profiles = TranslationProfile.GetBuiltInProfiles();
            var activeProfile = profiles.FirstOrDefault(p => p.Id == settings.ActiveProfileId) ?? profiles.FirstOrDefault();
            
            var request = new TranslationRequest
            {
                SourceText = text,
                TargetLanguage = settings.TargetLanguage,
                Profile = settings.UseAutoProfileDetection ? null : activeProfile,
                UseAutoDetection = settings.UseAutoProfileDetection
            };

            var result = await _translationService.TranslateAsync(request, CancellationToken.None);

            if (result.Success)
            {
                return (true, result.TranslatedText, null);
            }
            else
            {
                return (false, null, result.ErrorMessage ?? LocalizationService.Instance["TranslationFailed"]);
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
        StatusMessage = LocalizationService.Instance["Ready"];
        HasError = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelCurrentOperation();
        StatusMessage = LocalizationService.Instance["Cancelled"];
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

    private void SaveToHistory(string translatedText, string? detectedLanguage)
    {
        try
        {
            var historyItem = new TranslationHistoryItem
            {
                SourceText = SourceText,
                TranslatedText = translatedText,
                SourceLanguage = detectedLanguage ?? "auto",
                TargetLanguage = SelectedTargetLanguage,
                ProviderName = SelectedProvider?.Name,
                ProfileId = UseAutoProfileDetection ? "auto" : SelectedProfile?.Id
            };
            
            _historyService.Add(historyItem);
            _logger.Information("Translation saved to history");
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to save translation to history");
        }
    }

    public async Task TranslateTextAsync(string text)
    {
        SourceText = text;
        await TranslateCurrentTextAsync();
    }

    public void ShowNoTextSelectedError()
    {
        ShowError(LocalizationService.Instance["NoTextSelected"]);
    }

    partial void OnSelectedTargetLanguageChanged(string value)
    {
        var settings = _settingsStore.Load();
        settings.TargetLanguage = value;
        _settingsStore.Save(settings);
        UpdateCanSpeak();
    }

    partial void OnTranslatedTextChanged(string value)
    {
        OnPropertyChanged(nameof(TranslatedCharacterCount));
        UpdateCanSpeak();
    }

    private void UpdateCanSpeak()
    {
        var langCode = LanguageNormalizer.Normalize(SelectedTargetLanguage);
        CanSpeak = !string.IsNullOrWhiteSpace(TranslatedText) && _ttsService.IsLanguageSupported(langCode);
    }

    partial void OnSelectedProfileChanged(TranslationProfile? value)
    {
        if (value != null)
        {
            var settings = _settingsStore.Load();
            settings.ActiveProfileId = value.Id;
            _settingsStore.Save(settings);
            _logger.Information("Translation profile changed to: {ProfileId}", value.Id);
        }
    }

    partial void OnUseAutoProfileDetectionChanged(bool value)
    {
        var settings = _settingsStore.Load();
        settings.UseAutoProfileDetection = value;
        _settingsStore.Save(settings);
        _logger.Information("Auto profile detection changed to: {Value}", value);
    }

    partial void OnSourceTextChanged(string value)
    {
        OnPropertyChanged(nameof(SourceCharacterCount));
    }


    [RelayCommand]
    private void SwapTexts()
    {
        if (!string.IsNullOrEmpty(TranslatedText))
        {
            var temp = SourceText;
            SourceText = TranslatedText;
            TranslatedText = temp;
            StatusMessage = LocalizationService.Instance["TextsSwapped"];
        }
    }

    [RelayCommand]
    private void Paste()
    {
        var text = _clipboardService.GetText();
        if (!string.IsNullOrEmpty(text))
        {
            SourceText = text;
            StatusMessage = LocalizationService.Instance["PastedFromClipboard"];
        }
    }
}
