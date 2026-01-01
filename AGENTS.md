# QuickTranslate - Agent Documentation

This guide helps AI agents work effectively in the QuickTranslate repository.

## Project Overview

QuickTranslate is a Windows desktop application for text translation using OpenAI-compatible APIs. It provides global hotkeys, multiple AI provider support, translation history, and TTS capabilities.

**Tech Stack:**
- **Framework**: WPF (.NET 8.0-windows)
- **UI Library**: WPF-UI (Fluent Design)
- **MVVM**: CommunityToolkit.Mvvm
- **DI**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog
- **Audio**: NAudio (WAV playback)

## Build & Run Commands

```bash
# Restore dependencies
dotnet restore

# Build entire solution
dotnet build

# Run the desktop application
dotnet run --project QuickTranslate.Desktop

# Build release version
dotnet publish QuickTranslate.Desktop -c Release -r win-x64 --self-contained false
```

**Requirements:**
- Windows 10/11
- .NET 8.0 SDK

## Architecture

The solution is split into two projects:

```
QuickTranslate.sln
├── QuickTranslate.Core/       # Business logic, models, interfaces (net8.0-windows)
│   ├── Interfaces/           # Service interfaces
│   ├── Models/                # Data models
│   └── Services/              # Service implementations
└── QuickTranslate.Desktop/    # WPF application (net8.0-windows)
    ├── ViewModels/            # MVVM ViewModels
    ├── Views/                 # WPF Windows
    ├── Services/              # Desktop-specific services
    │   └── Interfaces/        # Desktop service interfaces
    ├── Converters/            # Value converters
    └── Resources/             # XAML themes and resource files
```

### Key Architectural Patterns

**MVVM Pattern:**
- ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Properties use `[ObservableProperty]` attribute for automatic property change notification
- Commands use `[RelayCommand]` attribute for automatic command creation
- Partial methods `OnPropertyNameChanged` are called automatically when properties change

**Dependency Injection:**
- Registered in `App.xaml.cs` using `Microsoft.Extensions.DependencyInjection`
- **Singletons**: Core services, MainViewModel, MainWindow
- **Transient**: SettingsViewModel, SettingsWindow, HistoryWindow
- Access services via `App.GetService<T>()` static method
- **IProviderClient** is a singleton that updates its internal provider configuration

**Service Layer Pattern:**
- Interfaces defined in respective `Interfaces/` folders
- Implementations in `Services/` folders
- Dependency injection ensures loose coupling

## Code Conventions

### File Structure
- File-scoped namespaces (no namespace braces)
- One public class/interface per file
- Interfaces start with `I` prefix
- Organized by logical layer (Models, Interfaces, Services, ViewModels, Views)

### Naming Conventions
- **Classes/PascalCase**: `MainViewModel`, `TranslationService`
- **Methods/PascalCase**: `TranslateAsync`, `LoadSettings`
- **Properties/PascalCase**: `SourceText`, `IsLoading`
- **Private fields/_camelCase**: `_translationService`, `_logger`
- **Constants/PascalCase**: `MaxRetryAttempts`, `DebounceMs`
- **Async methods**: End with `Async` suffix (e.g., `TranslateAsync`)
- **Command properties**: End with `Command` suffix (auto-generated from methods)

### MVVM Attributes (CommunityToolkit.Mvvm)
```csharp
public partial class MainViewModel : ObservableObject
{
    // Auto-generates SourceText property with PropertyChanged
    [ObservableProperty]
    private string _sourceText = string.Empty;

    // Auto-generates TranslateCommand from method
    [RelayCommand]
    private async Task TranslateAsync()
    {
        // Implementation
    }

    // Called automatically when SourceText changes
    partial void OnSourceTextChanged(string value)
    {
        // Handle change
    }
}
```

### C# Language Features
- **File-scoped namespaces**: `namespace QuickTranslate.Core.Services;`
- **ImplicitUsings**: Enabled - avoid redundant using statements
- **Nullable reference types**: Enabled - all types are nullable by default
- **Async/await**: Used for all async operations
- **Pattern matching**: Used extensively for conditionals
- **Collection expressions**: Used in `TranslationProfile.GetBuiltInProfiles()`

### Async Patterns
- All async methods accept `CancellationToken cancellationToken = default`
- Use `CancellationToken` for all async operations (HTTP, delays, etc.)
- Pass cancellation tokens from ViewModels to Services
- Cancel operations in ViewModels using `CancellationTokenSource`

```csharp
public async Task<TranslationResult> TranslateAsync(
    TranslationRequest request,
    CancellationToken cancellationToken = default)
{
    // Use cancellationToken in async calls
    var response = await httpClient.SendAsync(request, cancellationToken);
}
```

## Project Structure & Key Files

### QuickTranslate.Core

**Interfaces:**
- `IProviderClient.cs` - AI provider HTTP client interface
- `ISettingsStore.cs` - Settings persistence interface
- `ITranslationService.cs` - Translation orchestration interface
- `ITranslationHistoryService.cs` - History management interface
- `ITtsService.cs` - Text-to-speech interface

**Models:**
- `AppSettings.cs` - Application settings (providers, languages, hotkeys, theme)
- `ProviderConfig.cs` - AI provider configuration
- `TranslationRequest.cs` - Translation input parameters
- `TranslationResult.cs` - Translation output (with success/error)
- `TranslationProfile.cs` - Translation style profiles (General, Technical, etc.)
- `TranslationHistoryItem.cs` - History entry model
- `ChatCompletionRequest.cs` - OpenAI API request model

**Services:**
- `OpenAiProviderClient.cs` - OpenAI-compatible API client with retry logic
- `TranslationService.cs` - Translation orchestration, prompt building
- `SettingsStore.cs` - Settings persistence with DPAPI encryption and caching
- `TranslationHistoryService.cs` - History management (max 100 items)
- `PiperTtsService.cs` - Piper TTS API client
- `LanguageNormalizer.cs` - Language code normalization

### QuickTranslate.Desktop

**ViewModels:**
- `MainViewModel.cs` - Main window logic, translation commands
- `SettingsViewModel.cs` - Settings management
- `HistoryViewModel.cs` - Translation history display

**Views:**
- `MainWindow.xaml` - Main translation interface
- `SettingsWindow.xaml` - Settings UI
- `HistoryWindow.xaml` - History display
- `TranslationPopup.xaml` - Popup for hotkey translation

**Services:**
- `HotkeyService.cs` - Global hotkeys (Win32 API, RegisterHotKey)
- `ClipboardService.cs` - Clipboard operations (UI Automation + SendInput fallback)
- `AudioPlayerService.cs` - WAV audio playback (NAudio)
- `ThemeService.cs` - Theme management (OceanBlue, Emerald, Sunset, Purple, Monochrome)
- `LocalizationService.cs` - Resource file access

**Converters:**
- `BooleanConverters.cs` - Boolean to visibility/value converters

**Resources:**
- `Themes/*.xaml` - WPF-UI theme files (OceanBlue, Emerald, etc.)
- `Resources.resx` - Default resource strings (Russian)
- `Resources.en.resx` - English translations
- `Resources.os.resx` - Ossetian translations

## Dependencies & Key Libraries

### MVVM & UI
- **CommunityToolkit.Mvvm** (8.2.2) - MVVM with source generators
- **WPF-UI** (4.1.0) - Fluent Design UI components
- **Hardcodet.NotifyIcon.Wpf** (1.1.0) - System tray integration

### Core
- **Microsoft.Extensions.DependencyInjection** (8.0.0) - DI container
- **Microsoft.Extensions.Http** (8.0.0) - HTTP client factory
- **System.Text.Json** (8.0.5) - JSON serialization
- **System.Security.Cryptography.ProtectedData** (8.0.0) - Windows DPAPI

### Utilities
- **Serilog** (4.0.1) - Structured logging
- **Serilog.Sinks.File** (6.0.0) - File logging
- **HtmlAgilityPack** (1.11.61) - HTML parsing
- **NAudio** (2.2.1) - Audio playback (replaced System.Media.SoundPlayer)

## Configuration & Settings

### Settings Storage
- **Location**: `%LOCALAPPDATA%\QuickTranslate\settings.json`
- **Encryption**: API key encrypted with Windows DPAPI
- **Caching**: `SettingsStore` caches settings in memory with file watch
- **Cache Invalidation**: Cache invalidates when file modification time changes
- **Thread-Safety**: Lock-based synchronization in SettingsStore
- **Model**: `AppSettings` class (loaded/saved via `ISettingsStore`)

### Settings Structure
```csharp
public class AppSettings
{
    public List<ProviderConfig> Providers { get; set; }
    public string? ActiveProviderId { get; set; }
    public string TargetLanguage { get; set; }
    public string? InterfaceLanguage { get; set; } // "ru", "en", "os"
    public string? ColorTheme { get; set; } // "OceanBlue", "Emerald", etc.
    public string ActiveProfileId { get; set; } // "general", "technical", etc.
    public bool UseAutoProfileDetection { get; set; }
    public bool TtsEnabled { get; set; }
    public bool AutoSpeakAfterTranslate { get; set; }
    public HotkeyConfig TranslateSelectionHotkey { get; set; }
    public HotkeyConfig ShowHideHotkey { get; set; }
}
```

### Logging
- **Location**: `%LOCALAPPDATA%\QuickTranslate\logs\log-.txt`
- **Rotation**: Daily files, retained for 7 days
- **Format**: Timestamp, Level, Message, Exception
- **Output Template**: `{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}`
- **Usage**: `Log.ForContext<ClassName>()` to create context logger
- **Configured in**: `App.xaml.cs`

```csharp
private readonly ILogger _logger = Log.ForContext<MyClass>();

_logger.Information("Translation started");
_logger.Error(ex, "Translation failed");
```

## Localization

### Resource Files
- `Resources.resx` - Russian (default)
- `Resources.en.resx` - English
- `Resources.os.resx` - Ossetian

### Supported Interface Languages
- **Russian (ru)**: Default language
- **English (en)**: Supported
- **Ossetian (os)**: Supported

### Accessing Resources
**In C# code:**
```csharp
LocalizationService.Instance["Ready"]
LocalizationService.Instance["Translating"]
```

**In XAML:**
```xml
<TextBlock Text="{Binding [Translate], Source={x:Static local:Loc.Instance}}"/>
```

**Adding new strings:**
1. Add to all `.resx` files with same key
2. Use descriptive keys (e.g., "Translate", "Settings", "Ready")
3. Access via `LocalizationService.Instance["Key"]`

**Language Auto-Detection:**
- On first run, `LocalizationService` detects system language
- Falls back to English if system language not supported (ru, en, os)
- Saved language is persisted in `AppSettings.InterfaceLanguage`

## Important Patterns

### HTTP Client Usage
- Use `IHttpClientFactory` for connection pooling
- Named client: `"OpenAI"`
- Authorization header set per-request (not on HttpClient)
- Retry logic: 3 attempts with exponential backoff (1s, 2s, 4s)
- Retryable errors: 429, 500, 502, 503, 504, timeout

```csharp
services.AddHttpClient("OpenAI");

// In service
var client = httpClientFactory.CreateClient("OpenAI");
client.Timeout = TimeSpan.FromSeconds(timeout);
```

### Global Hotkeys
- Implementation: `HotkeyService.cs`
- Win32 API: `RegisterHotKey`, `UnregisterHotKey`
- Message handling: WndProc hook via HwndSource
- Debouncing: 300ms between hotkey triggers
- Modifier constants:
  - `Modifiers.Alt = 0x0001`
  - `Modifiers.Control = 0x0002`
  - `Modifiers.Shift = 0x0004`
  - `Modifiers.Win = 0x0008`
  - `Modifiers.NoRepeat = 0x4000`
- Key codes:
  - `Keys.T = 0x54`
  - `Keys.O = 0x4F`

### Clipboard Operations
- Implementation: `ClipboardService.cs`
- **Primary method**: UI Automation to get selected text directly
- **Fallback method**: SendInput simulation (Ctrl+C) to copy text
- **Thread attachment**: `AttachThreadInput` for reliable cross-thread focus
- **Clipboard preservation**: Backs up and restores clipboard content
- **Modifier key release**: Releases held Ctrl/Shift/Alt before SendInput
- Win32 API for clipboard access

### API Key Security
- Encrypted with Windows DPAPI (`System.Security.Cryptography.ProtectedData`)
- Stored encrypted in settings.json
- Cannot be read on other machines
- Encryption/decryption in `SettingsStore.cs`

### Text-to-Speech
- Backend: Self-hosted Piper TTS (`https://tts.rox-net.ru`)
- Supported languages: ru, en, de, es, fr, it, hi
- API: `POST /{lang}/api/tts` with JSON body
- Response: Raw WAV binary
- Playback: NAudio library (WaveOutEvent)
- Audio service supports stopping playback mid-stream

### Theme Service
- Singleton: `ThemeService.Instance`
- Available themes: OceanBlue, Emerald, Sunset, Purple, Monochrome
- Each theme defines: AccentColor, PrimaryAccent, SecondaryAccent, TertiaryAccent
- Uses `Wpf.Ui.Appearance.ApplicationAccentColorManager`
- Applies `ApplicationTheme.Dark` with `WindowBackdropType.Mica`
- Theme saved in `AppSettings.ColorTheme`
- Raises `ThemeChanged` event when theme changes

## Global Hotkeys

Default hotkeys (configurable via settings):
- **Ctrl+Shift+T (0x0006 + 0x54)**: Translate selected text
- **Ctrl+Shift+O (0x0006 + 0x4F)**: Show/hide main window

## Translation Profiles

Built-in profiles (defined in `TranslationProfile.cs`):
- **general**: General-purpose translation (no hints)
- **technical**: Technical documentation - preserves code, API names, technical terms
- **literary**: Literary/fiction - preserves style, adapts idioms naturally
- **legal**: Legal documents - formal legal terminology, precise wording
- **medical**: Medical text - proper medical terminology, Latin terms
- **casual**: Casual conversation - conversational tone, slang acceptable

Each profile has a `SystemPromptHint` appended to translation prompt. Profile names are localized using `NameKey` (e.g., "Profile_General", "Profile_Technical").

**Auto Profile Detection:**
- When enabled, AI analyzes text type and applies appropriate style
- Toggle setting: `UseAutoProfileDetection`
- AI determines text domain: technical, literary, legal, medical, casual, general
- Auto-detection prompt includes style guidance for each domain

## Language Support

### Translation Target Languages
The `AvailableLanguages` array in `MainViewModel` provides suggestions:
- Russian, English, German, French, Spanish, Chinese, Japanese, Korean

### Language Normalization
`LanguageNormalizer` supports 15 languages:
- **Translation**: ru, en, de, es, fr, it, hi, zh, ja, ko, pt, ar, tr, pl, uk
- **TTS Only**: ru, en, de, es, fr, it, hi

Normalization maps user input (language names, codes) to 2-letter codes:
- Supports English and Russian language names
- Partial matches: "ru", "русский", "russian" → "ru"
- Falls back to "ru" for unknown languages

### TTS Language Support
Only these languages are supported for text-to-speech:
- ru, en, de, es, fr, it, hi
- Other languages fallback to "ru" automatically

## Common Tasks

### Adding a New Provider Configuration
1. User adds via Settings UI
2. Creates `ProviderConfig` with Name, BaseUrl, ApiKey, Model, etc.
3. Settings saved via `ISettingsStore.Save(settings)`
4. Provider becomes available in MainViewModel's Providers list
5. Active provider saved in `AppSettings.ActiveProviderId`

### Updating IProviderClient
When user changes provider:
```csharp
_providerClient.UpdateProvider(newProvider);
```
This updates the singleton's internal configuration without recreating the service.

### Adding Translation to History
```csharp
var historyItem = new TranslationHistoryItem
{
    SourceText = sourceText,
    TranslatedText = translatedText,
    SourceLanguage = detectedLanguage,
    TargetLanguage = targetLanguage,
    ProviderName = providerName,
    ProfileId = profileId
};
_historyService.Add(historyItem);
```
History auto-saves after every successful translation.

### Updating Settings
```csharp
var settings = _settingsStore.Load();
settings.TargetLanguage = "English";
_settingsStore.Save(settings);
```

### Handling Errors in ViewModels
```csharp
try
{
    var result = await _translationService.TranslateAsync(request, cancellationToken);
    if (result.Success)
    {
        TranslatedText = result.TranslatedText;
        StatusMessage = "Translation complete";
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
    ShowError(ex.Message);
}
```

### Changing Theme
```csharp
ThemeService.Instance.SetTheme(AppTheme.Emerald);

// Settings are automatically saved
var settings = _settingsStore.Load();
settings.ColorTheme = "Emerald";
_settingsStore.Save(settings);
```

### Changing Interface Language
```csharp
LocalizationService.Instance.SetCulture("en");

// Settings are automatically saved
var settings = _settingsStore.Load();
settings.InterfaceLanguage = "en";
_settingsStore.Save(settings);
```

## Testing

**No test infrastructure currently exists in this project.**
- No test projects
- No test files
- No test framework configured
- No CI/CD pipeline

## Key Gotchas

1. **Platform-Specific**: This is Windows-only (Win32 API, WPF, DPAPI, NAudio)
2. **No Tests**: No automated tests exist
3. **ImplicitUsings**: Don't add redundant `using` statements
4. **File-Scoped Namespaces**: No namespace braces
5. **MVVM Attributes**: Use `[ObservableProperty]` and `[RelayCommand]` instead of manual INPC
6. **Dependency Injection**: All services must be registered in `App.xaml.cs`
7. **Settings Caching**: Settings are cached with file modification watch - changes trigger cache invalidation
8. **Thread-Safety**: SettingsStore uses lock-based synchronization
9. **Threading**: UI updates must happen on UI thread (use `Dispatcher` if needed)
10. **Clipboard**: UI Automation is primary method, SendInput is fallback with clipboard preservation
11. **Hotkey Debouncing**: 300ms debounce prevents double triggers
12. **Language Support**: TTS only supports: ru, en, de, es, fr, it, hi
13. **Resource Files**: Must update all .resx files when adding translations
14. **API Key**: Encrypted with DPAPI - cannot be read on other machines
15. **Retry Logic**: HTTP client has built-in retry for specific error codes (429, 500, 502, 503, 504)
16. **IProviderClient**: Singleton service that updates internal config via `UpdateProvider()`
17. **Audio Playback**: Uses NAudio WaveOutEvent, not System.Media.SoundPlayer
18. **Themes**: Always applies Dark theme with Mica backdrop, only accent colors vary
19. **Collection Expressions**: Used for built-in profiles (e.g., `new() { ... }`)
20. **Language ComboBox**: Editable ComboBox allows custom language input, not limited to suggestions

## Common Error Locations

- **Settings loading**: Missing or corrupted settings.json
- **API calls**: Invalid BaseUrl, missing/expired API key
- **Hotkey registration**: Conflicts with other applications
- **Clipboard**: UI Automation fails, SendInput blocked by elevated apps
- **TTS**: Unsupported language or network issues
- **Audio playback**: WAV format issues, concurrent playback conflicts
- **Theme application**: WPF-UI accent color manager initialization
- **History file**: Corrupted history.json or file permission issues

## Dependencies & Startup Flow

1. `App.xaml.cs` `Application_Startup`:
   - Configures Serilog logging (file sink, daily rotation)
   - Configures DI services
   - Initializes ThemeService (applies saved theme)
   - Creates and shows MainWindow

2. DI Registration order:
   - HttpClient factory (named client "OpenAI")
   - Core services (ISettingsStore, ITranslationHistoryService)
   - IProviderClient (singleton with active provider settings)
   - Core services (ITranslationService, ITtsService)
   - Desktop services (IHotkeyService, IClipboardService, IAudioPlayerService)
   - ViewModels and Windows

3. `MainWindow` initializes:
   - Gets MainViewModel from DI
   - DataContext set to MainViewModel
   - HotkeyService initialized with window handle
   - Settings loaded via MainViewModel.LoadSettings()
   - Hotkeys registered from AppSettings

## File-Scoped Namespace Example

```csharp
using System;
using Serilog;

namespace QuickTranslate.Core.Services;

public class TranslationService : ITranslationService
{
    // Implementation
}
```

## Localization Keys Pattern

Resource keys follow PascalCase and are descriptive:
- "Ready" - Initial state
- "Translating" - In-progress state
- "TranslationComplete" - Success state
- "TranslationFailed" - Error state
- "SourceText" - Label
- "Translate" - Button
- "Settings" - Menu item
- "History" - Menu item
- "Profile_General" - Translation profile name
- "Profile_Technical" - Translation profile name

## Translation History

**Storage:**
- Location: `%LOCALAPPDATA%\QuickTranslate\history.json`
- Max items: 100
- Favorites are preserved during cleanup
- Fields: source text, translated text, languages, provider, profile, timestamp, favorite flag

**Features:**
- Auto-save after each successful translation
- Search by source/translated text
- Filter by favorites
- Toggle favorite status
- Copy source/translation to clipboard
- Use history item (populates MainWindow)
- Clear history (preserves favorites)
- Timestamp formatting: "HH:mm" today, "Yesterday HH:mm", "dd MMM HH:mm" this year, else full date

## When to Ask User

- **Business Logic**: How should a new translation profile behave?
- **UI Changes**: What should a new feature look like?
- **Compatibility**: Support for new AI providers?
- **Breaking Changes**: Major architectural changes?

Otherwise, make autonomous decisions based on existing patterns.
