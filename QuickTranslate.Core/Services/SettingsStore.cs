using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using Serilog;

namespace QuickTranslate.Core.Services;

public class SettingsStore : ISettingsStore
{
    private readonly string _settingsPath;
    private readonly ILogger _logger;

    public SettingsStore()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "QuickTranslate");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
        _logger = Log.ForContext<SettingsStore>();
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.Information("Settings file not found, creating defaults");
                var defaults = CreateDefaultSettings();
                Save(defaults);
                return defaults;
            }

            var json = File.ReadAllText(_settingsPath);
            var stored = JsonSerializer.Deserialize<StoredAppSettings>(json);

            if (stored == null)
                return CreateDefaultSettings();

            var settings = new AppSettings
            {
                ActiveProviderId = stored.ActiveProviderId,
                TargetLanguage = stored.TargetLanguage ?? "Russian",
                InterfaceLanguage = stored.InterfaceLanguage,
                ColorTheme = stored.ColorTheme,
                ActiveProfileId = stored.ActiveProfileId ?? "general",
                UseAutoProfileDetection = stored.UseAutoProfileDetection,
                Providers = stored.Providers?.Select(sp => new ProviderConfig
                {
                    Id = sp.Id ?? Guid.NewGuid().ToString(),
                    Name = sp.Name ?? "Provider",
                    BaseUrl = sp.BaseUrl ?? "https://api.openai.com/v1",
                    Model = sp.Model ?? "gpt-4o-mini",
                    Temperature = sp.Temperature,
                    MaxTokens = sp.MaxTokens,
                    TimeoutSeconds = sp.TimeoutSeconds,
                    ApiKey = DecryptApiKey(sp.EncryptedApiKey)
                }).ToList() ?? new List<ProviderConfig>(),
                TranslateSelectionHotkey = stored.TranslateSelectionHotkey != null 
                    ? new HotkeyConfig(stored.TranslateSelectionHotkey.Modifiers, stored.TranslateSelectionHotkey.Key)
                    : new HotkeyConfig(0x0006, 0x54),
                ShowHideHotkey = stored.ShowHideHotkey != null 
                    ? new HotkeyConfig(stored.ShowHideHotkey.Modifiers, stored.ShowHideHotkey.Key)
                    : new HotkeyConfig(0x0006, 0x4F)
            };

            if (settings.Providers.Count == 0)
            {
                settings.Providers.Add(CreateDefaultProvider());
            }

            _logger.Information("Settings loaded: {Count} providers", settings.Providers.Count);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load settings");
            return CreateDefaultSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var stored = new StoredAppSettings
            {
                ActiveProviderId = settings.ActiveProviderId,
                TargetLanguage = settings.TargetLanguage,
                InterfaceLanguage = settings.InterfaceLanguage,
                ColorTheme = settings.ColorTheme,
                ActiveProfileId = settings.ActiveProfileId,
                UseAutoProfileDetection = settings.UseAutoProfileDetection,
                Providers = settings.Providers.Select(p => new StoredProviderConfig
                {
                    Id = p.Id,
                    Name = p.Name,
                    BaseUrl = p.BaseUrl,
                    Model = p.Model,
                    Temperature = p.Temperature,
                    MaxTokens = p.MaxTokens,
                    TimeoutSeconds = p.TimeoutSeconds,
                    EncryptedApiKey = EncryptApiKey(p.ApiKey)
                }).ToList(),
                TranslateSelectionHotkey = new StoredHotkeyConfig 
                { 
                    Modifiers = settings.TranslateSelectionHotkey.Modifiers, 
                    Key = settings.TranslateSelectionHotkey.Key 
                },
                ShowHideHotkey = new StoredHotkeyConfig 
                { 
                    Modifiers = settings.ShowHideHotkey.Modifiers, 
                    Key = settings.ShowHideHotkey.Key 
                }
            };

            var json = JsonSerializer.Serialize(stored, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
            _logger.Information("Settings saved: {Count} providers", settings.Providers.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save settings");
            throw;
        }
    }

    private AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            TargetLanguage = "Russian",
            Providers = new List<ProviderConfig> { CreateDefaultProvider() }
        };
    }

    private ProviderConfig CreateDefaultProvider()
    {
        return new ProviderConfig
        {
            Id = Guid.NewGuid().ToString(),
            Name = "OpenAI",
            BaseUrl = "https://api.openai.com/v1",
            Model = "gpt-4o-mini",
            Temperature = 0.3,
            MaxTokens = 4096,
            TimeoutSeconds = 60
        };
    }

    private string EncryptApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return string.Empty;

        try
        {
            var data = Encoding.UTF8.GetBytes(apiKey);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to encrypt API key");
            return string.Empty;
        }
    }

    private string DecryptApiKey(string? encryptedKey)
    {
        if (string.IsNullOrEmpty(encryptedKey))
            return string.Empty;

        try
        {
            var encrypted = Convert.FromBase64String(encryptedKey);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to decrypt API key");
            return string.Empty;
        }
    }

    private class StoredAppSettings
    {
        public string? ActiveProviderId { get; set; }
        public string? TargetLanguage { get; set; }
        public string? InterfaceLanguage { get; set; }
        public string? ColorTheme { get; set; }
        public string? ActiveProfileId { get; set; }
        public bool UseAutoProfileDetection { get; set; }
        public List<StoredProviderConfig>? Providers { get; set; }
        public StoredHotkeyConfig? TranslateSelectionHotkey { get; set; }
        public StoredHotkeyConfig? ShowHideHotkey { get; set; }
    }

    private class StoredHotkeyConfig
    {
        public uint Modifiers { get; set; }
        public uint Key { get; set; }
    }

    private class StoredProviderConfig
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? BaseUrl { get; set; }
        public string? EncryptedApiKey { get; set; }
        public string? Model { get; set; }
        public double Temperature { get; set; } = 0.3;
        public int MaxTokens { get; set; } = 4096;
        public int TimeoutSeconds { get; set; } = 60;
    }
}
