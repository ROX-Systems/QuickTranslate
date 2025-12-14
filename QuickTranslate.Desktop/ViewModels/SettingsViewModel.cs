using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using Serilog;

namespace QuickTranslate.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly IProviderClient _providerClient;
    private readonly ILogger _logger;
    private AppSettings _appSettings = new();

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
    private double _temperature = 0.3;

    [ObservableProperty]
    private int _maxTokens = 4096;

    [ObservableProperty]
    private int _timeoutSeconds = 60;

    [ObservableProperty]
    private string _targetLanguage = "Russian";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private bool _hasSelectedProvider;

    public string[] AvailableLanguages { get; } = { "Russian", "English", "German", "French", "Spanish", "Chinese", "Japanese", "Korean" };

    public event EventHandler? SettingsSaved;
    public event EventHandler? CloseRequested;

    public SettingsViewModel(ISettingsStore settingsStore, IProviderClient providerClient)
    {
        _settingsStore = settingsStore;
        _providerClient = providerClient;
        _logger = Log.ForContext<SettingsViewModel>();
        
        LoadSettings();
    }

    private void LoadSettings()
    {
        _appSettings = _settingsStore.Load();
        
        Providers = new ObservableCollection<ProviderConfig>(_appSettings.Providers);
        TargetLanguage = _appSettings.TargetLanguage;

        var activeProvider = _appSettings.GetActiveProvider();
        if (activeProvider != null)
        {
            SelectedProvider = Providers.FirstOrDefault(p => p.Id == activeProvider.Id);
        }
        else if (Providers.Count > 0)
        {
            SelectedProvider = Providers[0];
        }

        _logger.Information("Settings loaded: {Count} providers", Providers.Count);
    }

    partial void OnSelectedProviderChanged(ProviderConfig? value)
    {
        HasSelectedProvider = value != null;
        
        if (value != null)
        {
            ProviderName = value.Name;
            BaseUrl = value.BaseUrl;
            ApiKey = value.ApiKey;
            Model = value.Model;
            Temperature = value.Temperature;
            MaxTokens = value.MaxTokens;
            TimeoutSeconds = value.TimeoutSeconds;
        }
        else
        {
            ProviderName = string.Empty;
            BaseUrl = "https://api.openai.com/v1";
            ApiKey = string.Empty;
            Model = "gpt-4o-mini";
            Temperature = 0.3;
            MaxTokens = 4096;
            TimeoutSeconds = 60;
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

        SelectedProvider.Name = ProviderName;
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

            _appSettings.Providers = Providers.ToList();
            _appSettings.TargetLanguage = TargetLanguage;
            
            if (SelectedProvider != null)
            {
                _appSettings.ActiveProviderId = SelectedProvider.Id;
                _providerClient.UpdateProvider(SelectedProvider);
            }

            _settingsStore.Save(_appSettings);
            
            StatusMessage = "Settings saved!";
            _logger.Information("Settings saved: {Count} providers", Providers.Count);
            
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
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "Please enter an API key";
            return;
        }

        IsTesting = true;
        StatusMessage = "Testing connection...";

        try
        {
            var testProvider = new ProviderConfig
            {
                BaseUrl = BaseUrl,
                ApiKey = ApiKey,
                Model = Model,
                Temperature = Temperature,
                MaxTokens = 50,
                TimeoutSeconds = 30
            };

            _providerClient.UpdateProvider(testProvider);

            var result = await _providerClient.SendTranslationRequestAsync(
                "You are a test assistant.",
                "Say 'OK' if you can read this.",
                CancellationToken.None);

            if (result.Success)
            {
                StatusMessage = "✓ Connection successful!";
                _logger.Information("Connection test passed for {Provider}", ProviderName);
            }
            else
            {
                StatusMessage = $"✗ Test failed: {result.ErrorMessage}";
                _logger.Warning("Connection test failed: {Error}", result.ErrorMessage);
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
