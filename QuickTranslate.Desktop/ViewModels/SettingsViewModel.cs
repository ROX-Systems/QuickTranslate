using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using QuickTranslate.Core.Validators;
using QuickTranslate.Desktop.Services;
using Serilog;

namespace QuickTranslate.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly IProviderClient _providerClient;
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger _logger;
    private readonly AppSettingsValidator _appSettingsValidator;
    private readonly ProviderConfigValidator _providerValidator;
    private AppSettings _appSettings = new();

    [ObservableProperty]
    private string _validationError = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ProviderConfig> _providers = new();

    [ObservableProperty]
    private ProviderConfig? _selectedProvider;

    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _baseUrl = "https://api.openai.com/v1";

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _model = "gpt-4o-mini";

    [ObservableProperty]
    private ProviderType _providerType = ProviderType.OpenAI;

    [ObservableProperty]
    private double _temperature = 0.3;

    [ObservableProperty]
    private int _maxTokens = 4096;

    [ObservableProperty]
    private int _timeoutSeconds = 60;

    // Flag to prevent update cycles
    private bool _isUpdatingProperties = false;

    [ObservableProperty]
    private string _targetLanguage = "Russian";

    [ObservableProperty]
    private string _selectedInterfaceLanguage = "ru";

    [ObservableProperty]
    private AppTheme _selectedColorTheme = AppTheme.OceanBlue;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    partial void OnSelectedInterfaceLanguageChanged(string value)
    {
        Services.LocalizationService.Instance.SetCulture(value);
    }

    partial void OnSelectedColorThemeChanged(AppTheme value)
    {
        ThemeService.Instance.SetTheme(value);
    }

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private bool _hasSelectedProvider;

    [ObservableProperty]
    private string _translateSelectionHotkeyText = "Ctrl+Shift+T";

    [ObservableProperty]
    private string _showHideHotkeyText = "Ctrl+Shift+O";

    private HotkeyConfig _translateSelectionHotkey = new(0x0006, 0x54);
    private HotkeyConfig _showHideHotkey = new(0x0006, 0x4F);

    public string[] AvailableLanguages { get; } = { "Russian", "English", "German", "French", "Spanish", "Chinese", "Japanese", "Korean" };

    public ObservableCollection<LanguageOption> AvailableInterfaceLanguages { get; } = new()
    {
        new() { Code = "ru", Name = "Русский" },
        new() { Code = "os", Name = "Ирон" },
        new() { Code = "en", Name = "English" }
    };

    public List<ThemeInfo> AvailableColorThemes { get; } = ThemeService.AvailableThemes.Values.ToList();

    public ObservableCollection<ProviderTypeInfo> AvailableProviderTypes { get; } = new()
    {
        new() { Type = ProviderType.OpenAI, Name = "OpenAI Compatible", Description = "OpenAI API and compatible providers (z.ai, Groq, Together, etc.)" },
        new() { Type = ProviderType.Anthropic, Name = "Anthropic Claude", Description = "Anthropic Claude API" },
        new() { Type = ProviderType.Google, Name = "Google Gemini", Description = "Google Gemini API" },
        new() { Type = ProviderType.Ollama, Name = "Ollama", Description = "Local inference with Ollama" },
        new() { Type = ProviderType.Custom, Name = "Custom", Description = "Custom provider implementation" }
    };

    [ObservableProperty]
    private ProviderTypeInfo? _selectedProviderType;

    public event EventHandler? SettingsSaved;
    public event EventHandler? CloseRequested;

    public SettingsViewModel(ISettingsStore settingsStore, IProviderClient providerClient, IHealthCheckService healthCheckService)
    {
        _settingsStore = settingsStore;
        _providerClient = providerClient;
        _healthCheckService = healthCheckService;
        _logger = Log.ForContext<SettingsViewModel>();
        _appSettingsValidator = new AppSettingsValidator();
        _providerValidator = new ProviderConfigValidator();

        LoadSettings();
    }

    private void LoadSettings()
    {
        _appSettings = _settingsStore.Load();
        
        Providers = new ObservableCollection<ProviderConfig>(_appSettings.Providers);
        TargetLanguage = _appSettings.TargetLanguage;
        SelectedInterfaceLanguage = _appSettings.InterfaceLanguage ?? "ru";
        
        if (!string.IsNullOrEmpty(_appSettings.ColorTheme) && Enum.TryParse<AppTheme>(_appSettings.ColorTheme, out var theme))
        {
            SelectedColorTheme = theme;
        }

        _translateSelectionHotkey = _appSettings.TranslateSelectionHotkey;
        _showHideHotkey = _appSettings.ShowHideHotkey;
        TranslateSelectionHotkeyText = FormatHotkey(_translateSelectionHotkey);
        ShowHideHotkeyText = FormatHotkey(_showHideHotkey);

        var activeProvider = _appSettings.GetActiveProvider();
        if (activeProvider != null)
        {
            _logger.Information("Setting active provider: {Name} (Type: {Type})", activeProvider.Name, activeProvider.Type);
            SelectedProvider = Providers.FirstOrDefault(p => p.Id == activeProvider.Id);
        }
        else if (Providers.Count > 0)
        {
            _logger.Information("No active provider, selecting first provider");
            SelectedProvider = Providers[0];
        }

        _logger.Information("Settings loaded successfully with {Count} providers", Providers.Count);
    }

    private static string FormatHotkey(HotkeyConfig hotkey)
    {
        var parts = new List<string>();
        
        if ((hotkey.Modifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((hotkey.Modifiers & 0x0004) != 0) parts.Add("Shift");
        if ((hotkey.Modifiers & 0x0001) != 0) parts.Add("Alt");
        if ((hotkey.Modifiers & 0x0008) != 0) parts.Add("Win");
        
        var keyName = GetKeyName(hotkey.Key);
        parts.Add(keyName);
        
        return string.Join("+", parts);
    }

    private static string GetKeyName(uint vk)
    {
        return vk switch
        {
            >= 0x41 and <= 0x5A => ((char)vk).ToString(),
            >= 0x30 and <= 0x39 => ((char)vk).ToString(),
            >= 0x70 and <= 0x87 => $"F{vk - 0x6F}",
            0x20 => "Space",
            0x0D => "Enter",
            0x09 => "Tab",
            0x08 => "Backspace",
            0x2E => "Delete",
            0x24 => "Home",
            0x23 => "End",
            0x21 => "PageUp",
            0x22 => "PageDown",
            0x26 => "Up",
            0x28 => "Down",
            0x25 => "Left",
            0x27 => "Right",
            0x2D => "Insert",
            0xC0 => "`",
            0xBD => "-",
            0xBB => "=",
            0xDB => "[",
            0xDD => "]",
            0xDC => "\\",
            0xBA => ";",
            0xDE => "'",
            0xBC => ",",
            0xBE => ".",
            0xBF => "/",
            _ => $"Key{vk:X2}"
        };
    }

    public void SetTranslateSelectionHotkey(uint modifiers, uint key)
    {
        _translateSelectionHotkey = new HotkeyConfig(modifiers, key);
        TranslateSelectionHotkeyText = FormatHotkey(_translateSelectionHotkey);
    }

    public void SetShowHideHotkey(uint modifiers, uint key)
    {
        _showHideHotkey = new HotkeyConfig(modifiers, key);
        ShowHideHotkeyText = FormatHotkey(_showHideHotkey);
    }

    partial void OnSelectedProviderChanged(ProviderConfig? value)
    {
        try
        {
            _logger.Debug("OnSelectedProviderChanged: {Provider}", value?.Name ?? "null");

            HasSelectedProvider = value != null;

            if (value != null)
            {
                _isUpdatingProperties = true;

                ProviderName = value.Name;

                // Validate and fix ProviderType
                if (!Enum.IsDefined(typeof(ProviderType), value.Type))
                {
                    _logger.Warning("Provider has invalid type {Type}, setting to OpenAI", value.Type);
                    ProviderType = ProviderType.OpenAI;
                }
                else
                {
                    ProviderType = value.Type;
                }

                SelectedProviderType = AvailableProviderTypes.FirstOrDefault(p => p.Type == ProviderType);

                // If type not found in list, default to OpenAI
                if (SelectedProviderType == null)
                {
                    _logger.Warning("Provider type {Type} not found in available types, defaulting to OpenAI", value.Type);
                    SelectedProviderType = AvailableProviderTypes.FirstOrDefault(p => p.Type == ProviderType.OpenAI);
                    ProviderType = ProviderType.OpenAI;
                }

                // Only update other fields if not already updating (to prevent cycles)
                BaseUrl = value.BaseUrl ?? "https://api.openai.com/v1";
                ApiKey = value.ApiKey ?? string.Empty;
                Model = value.Model ?? "gpt-4o-mini";
                Temperature = value.Temperature >= 0 && value.Temperature <= 2 ? value.Temperature : 0.3;
                MaxTokens = value.MaxTokens > 0 ? value.MaxTokens : 4096;
                TimeoutSeconds = value.TimeoutSeconds > 0 && value.TimeoutSeconds <= 300 ? value.TimeoutSeconds : 60;

                _isUpdatingProperties = false;
            }
            else
            {
                _isUpdatingProperties = true;

                ProviderName = string.Empty;
                ProviderType = ProviderType.OpenAI;
                SelectedProviderType = AvailableProviderTypes.FirstOrDefault(p => p.Type == ProviderType.OpenAI);
                BaseUrl = "https://api.openai.com/v1";
                ApiKey = string.Empty;
                Model = "gpt-4o-mini";
                Temperature = 0.3;
                MaxTokens = 4096;
                TimeoutSeconds = 60;

                _isUpdatingProperties = false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in OnSelectedProviderChanged");
            _isUpdatingProperties = false;
            throw;
        }
    }

    partial void OnSelectedProviderTypeChanged(ProviderTypeInfo? value)
    {
        if (value != null)
        {
            ProviderType = value.Type;
        }
    }

    partial void OnProviderTypeChanged(ProviderType value)
    {
        try
        {
            // Skip if we're already updating properties from provider selection
            if (_isUpdatingProperties)
            {
                _logger.Debug("OnProviderTypeChanged: Skipping (already updating)");
                return;
            }

            // Only update defaults if we're in a clean state (loading new provider)
            // This prevents overriding values when loading existing providers
            _logger.Debug("OnProviderTypeChanged: {Type}", value);

            // Set default values based on provider type
            // NOTE: For OpenAI Compatible (ProviderType.OpenAI), we don't override
            // values because users may have custom providers like z.ai with specific URLs
            switch (value)
            {
                case ProviderType.OpenAI:
                    // Don't override for OpenAI - users may have custom URLs
                    // Only set if fields are empty
                    if (string.IsNullOrEmpty(BaseUrl))
                        BaseUrl = "https://api.openai.com/v1";
                    if (string.IsNullOrEmpty(Model))
                        Model = "gpt-4o-mini";
                    break;

                case ProviderType.Ollama:
                    BaseUrl = "http://localhost:11434/v1";
                    Model = "llama3";
                    ApiKey = string.Empty; // Ollama doesn't require API key
                    break;

                case ProviderType.Anthropic:
                    BaseUrl = "https://api.anthropic.com/v1";
                    Model = "claude-3-haiku-20240307";
                    break;

                case ProviderType.Google:
                    BaseUrl = "https://generativelanguage.googleapis.com/v1";
                    Model = "gemini-pro";
                    break;

                case ProviderType.Custom:
                    // Don't override for Custom providers
                    break;

                default:
                    if (string.IsNullOrEmpty(BaseUrl))
                        BaseUrl = "https://api.openai.com/v1";
                    if (string.IsNullOrEmpty(Model))
                        Model = "gpt-4o-mini";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in OnProviderTypeChanged");
        }
    }

    [RelayCommand]
    private void AddProvider()
    {
        var newProvider = new ProviderConfig
        {
            Name = $"Provider {Providers.Count + 1}"
        };
        
        Providers.Add(newProvider);
        SelectedProvider = newProvider;
        StatusMessage = "New provider added. Configure and save.";
        _logger.Information("New provider added: {Name}", newProvider.Name);
    }

    [RelayCommand]
    private void DeleteProvider()
    {
        if (SelectedProvider == null) return;
        
        if (Providers.Count <= 1)
        {
            StatusMessage = "Cannot delete the last provider";
            return;
        }

        var toDelete = SelectedProvider;
        var index = Providers.IndexOf(toDelete);
        
        Providers.Remove(toDelete);
        SelectedProvider = Providers.Count > index ? Providers[index] : Providers.LastOrDefault();
        
        StatusMessage = $"Provider '{toDelete.Name}' deleted";
        _logger.Information("Provider deleted: {Name}", toDelete.Name);
    }

    [RelayCommand]
    private void DuplicateProvider()
    {
        if (SelectedProvider == null) return;

        var clone = SelectedProvider.Clone();
        clone.Id = Guid.NewGuid().ToString();
        clone.Name = $"{SelectedProvider.Name} (Copy)";
        
        Providers.Add(clone);
        SelectedProvider = clone;
        StatusMessage = "Provider duplicated";
    }

    private void UpdateSelectedProviderFromFields()
    {
        if (SelectedProvider == null) return;

        var tempProvider = new ProviderConfig
        {
            Name = ProviderName,
            Type = ProviderType,
            BaseUrl = BaseUrl,
            ApiKey = ApiKey,
            Model = Model,
            Temperature = Temperature,
            MaxTokens = MaxTokens,
            TimeoutSeconds = TimeoutSeconds
        };

        var validationResult = _providerValidator.Validate(tempProvider);
        if (!validationResult.IsValid)
        {
            ValidationError = string.Join(Environment.NewLine, validationResult.Errors.Select(e => e.ErrorMessage));
            return;
        }

        ValidationError = string.Empty;

        SelectedProvider.Name = ProviderName;
        SelectedProvider.Type = ProviderType;
        SelectedProvider.BaseUrl = BaseUrl;
        SelectedProvider.ApiKey = ApiKey;
        SelectedProvider.Model = Model;
        SelectedProvider.Temperature = Temperature;
        SelectedProvider.MaxTokens = MaxTokens;
        SelectedProvider.TimeoutSeconds = TimeoutSeconds;

        var index = Providers.IndexOf(SelectedProvider);
        if (index >= 0)
        {
            OnPropertyChanged(nameof(Providers));
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            UpdateSelectedProviderFromFields();

            if (!string.IsNullOrWhiteSpace(ValidationError))
            {
                StatusMessage = ValidationError;
                return;
            }

            _appSettings.Providers = Providers.ToList();
            _appSettings.TargetLanguage = TargetLanguage;
            _appSettings.InterfaceLanguage = SelectedInterfaceLanguage;
            _appSettings.ColorTheme = SelectedColorTheme.ToString();
            _appSettings.TranslateSelectionHotkey = _translateSelectionHotkey;
            _appSettings.ShowHideHotkey = _showHideHotkey;

            if (SelectedProvider != null)
            {
                _appSettings.ActiveProviderId = SelectedProvider.Id;
            }

            var appSettingsValidationResult = _appSettingsValidator.Validate(_appSettings);
            if (!appSettingsValidationResult.IsValid)
            {
                var firstError = appSettingsValidationResult.Errors.First();
                StatusMessage = firstError.ErrorMessage;
                return;
            }

            if (SelectedProvider != null)
            {
                _providerClient.UpdateProvider(SelectedProvider);
            }

            _settingsStore.Save(_appSettings);

            StatusMessage = "Settings saved!";
            _logger.Information("Settings saved: {Count} providers", Providers.Count);
            _logger.Debug("Active provider API key length: {Length} chars",
                SelectedProvider?.ApiKey?.Length ?? 0);

            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
            _logger.Error(ex, "Failed to save settings");
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        var tempProvider = new ProviderConfig
        {
            Name = ProviderName,
            BaseUrl = BaseUrl,
            ApiKey = ApiKey,
            Model = Model,
            Temperature = Temperature,
            MaxTokens = 50,
            TimeoutSeconds = 30
        };

        var validationResult = _providerValidator.Validate(tempProvider);
        if (!validationResult.IsValid)
        {
            StatusMessage = validationResult.Errors.First().ErrorMessage;
            return;
        }

        IsTesting = true;
        StatusMessage = "Testing connection...";

        _logger.Debug("Testing connection with API key length: {Length} chars",
            tempProvider.ApiKey?.Length ?? 0);

        try
        {
            var healthResult = await _healthCheckService.CheckProviderHealthAsync(tempProvider, CancellationToken.None);

            if (healthResult.Status == HealthStatus.Healthy)
            {
                StatusMessage = $"✓ Connection successful! {healthResult.Description}";
                _logger.Information("Connection test passed for {Provider}", ProviderName);
            }
            else
            {
                StatusMessage = $"✗ Test failed: {healthResult.Description}";
                _logger.Warning("Connection test failed for {Provider}: {Error}", ProviderName, healthResult.Description);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
            _logger.Error(ex, "Connection test error");
        }
        finally
        {
            IsTesting = false;

            var activeProvider = _appSettings.GetActiveProvider();
            if (activeProvider != null)
            {
                _providerClient.UpdateProvider(activeProvider);
            }
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        LoadSettings();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
