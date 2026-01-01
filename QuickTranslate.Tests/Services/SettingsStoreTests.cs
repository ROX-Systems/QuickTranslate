using FluentAssertions;
using QuickTranslate.Core.Models;
using QuickTranslate.Core.Services;
using Xunit;

namespace QuickTranslate.Tests.Services;

public class SettingsStoreTests
{
    [Fact]
    public void Load_WhenFileExists_ReturnsSavedSettings()
    {
        // Note: SettingsStore uses a fixed path in %LOCALAPPDATA%
        // This test verifies that settings can be loaded from existing file
        var settingsStore = new SettingsStore();
        var result = settingsStore.Load();

        result.Should().NotBeNull();
        result.Providers.Should().NotBeNull();
        // Note: TargetLanguage will be "Russian" by default or whatever was saved before
        // We cannot guarantee the exact value in this test
        result.TargetLanguage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SaveAndLoad_WhenSettingsSaved_LoadsCorrectly()
    {
        var settingsStore = new SettingsStore();
        var settings = new AppSettings
        {
            TargetLanguage = "English",
            InterfaceLanguage = "en",
            ColorTheme = "Emerald",
            ActiveProfileId = "technical",
            TtsEnabled = false,
            TtsEndpoint = "https://custom-tts.example.com"
        };

        settingsStore.Save(settings);
        var loaded = settingsStore.Load();

        loaded.TargetLanguage.Should().Be("English");
        loaded.InterfaceLanguage.Should().Be("en");
        loaded.ColorTheme.Should().Be("Emerald");
        loaded.ActiveProfileId.Should().Be("technical");
        loaded.TtsEnabled.Should().BeFalse();
        loaded.TtsEndpoint.Should().Be("https://custom-tts.example.com");
    }

    [Fact]
    public void SaveAndLoad_WhenProvidersSaved_LoadsCorrectly()
    {
        var settingsStore = new SettingsStore();
        var settings = new AppSettings
        {
            Providers = new List<ProviderConfig>
            {
                new ProviderConfig
                {
                    Id = "provider1",
                    Name = "Test Provider",
                    BaseUrl = "https://api.example.com/v1",
                    ApiKey = "test-key-12345",
                    Model = "gpt-4",
                    Temperature = 0.5,
                    MaxTokens = 2048,
                    TimeoutSeconds = 30
                }
            },
            ActiveProviderId = "provider1"
        };

        settingsStore.Save(settings);
        var loaded = settingsStore.Load();

        loaded.Providers.Should().HaveCountGreaterThan(0);
        var provider = loaded.Providers.FirstOrDefault(p => p.Id == "provider1");
        provider.Should().NotBeNull();
        provider!.Name.Should().Be("Test Provider");
        provider.BaseUrl.Should().Be("https://api.example.com/v1");
        provider.ApiKey.Should().Be("test-key-12345");
        provider.Model.Should().Be("gpt-4");
        provider.Temperature.Should().Be(0.5);
        provider.MaxTokens.Should().Be(2048);
        provider.TimeoutSeconds.Should().Be(30);
        loaded.ActiveProviderId.Should().Be("provider1");
    }

    [Fact]
    public void SaveAndLoad_WhenApiKeyIsEncrypted_KeyIsDecryptedCorrectly()
    {
        var settingsStore = new SettingsStore();
        var originalApiKey = "test-api-key-12345";
        var settings = new AppSettings
        {
            Providers = new List<ProviderConfig>
            {
                new ProviderConfig
                {
                    Id = "provider1",
                    Name = "Test",
                    ApiKey = originalApiKey
                }
            }
        };

        settingsStore.Save(settings);
        var loadedSettings = settingsStore.Load();
        var loadedApiKey = loadedSettings.Providers.First(p => p.Id == "provider1").ApiKey;

        loadedApiKey.Should().Be(originalApiKey);
    }

    [Fact]
    public void Load_WhenSettingsFileExists_CacheInvalidatesOnFileChange()
    {
        var settingsStore = new SettingsStore();
        var settings1 = new AppSettings { TargetLanguage = "English" };
        settingsStore.Save(settings1);

        var firstLoad = settingsStore.Load();
        firstLoad.TargetLanguage.Should().Be("English");

        var settings2 = new AppSettings { TargetLanguage = "German" };
        settingsStore.Save(settings2);

        var secondLoad = settingsStore.Load();
        secondLoad.TargetLanguage.Should().Be("German");
    }
}
