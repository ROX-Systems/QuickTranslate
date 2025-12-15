# QuickTranslate - Project Specifications

## Overview
Desktop translation application using AI providers (OpenAI-compatible APIs).

## Tech Stack
- **Framework**: WPF (.NET 8.0-windows)
- **UI Library**: WPF-UI (Fluent Design)
- **MVVM**: CommunityToolkit.Mvvm
- **Logging**: Serilog
- **DI**: Microsoft.Extensions.DependencyInjection

## Architecture
```
QuickTranslate.sln
├── QuickTranslate.Core/       # Business logic, models, interfaces (net8.0-windows)
│   ├── Interfaces/
│   │   ├── IProviderClient.cs
│   │   ├── ISettingsStore.cs
│   │   └── ITranslationService.cs
│   ├── Models/
│   │   ├── AppSettings.cs
│   │   ├── ChatCompletionRequest.cs
│   │   ├── ProviderConfig.cs
│   │   ├── TranslationProfile.cs
│   │   ├── TranslationRequest.cs
│   │   └── TranslationResult.cs
│   └── Services/
│       ├── OpenAiProviderClient.cs
│       ├── SettingsStore.cs
│       └── TranslationService.cs
└── QuickTranslate.Desktop/    # WPF application (net8.0-windows)
    ├── ViewModels/
    ├── Views/
    ├── Services/
    │   └── Interfaces/
    │       ├── IClipboardService.cs
    │       └── IHotkeyService.cs
    ├── Converters/
    └── Resources/
```

## Key Design Decisions

### Target Language Selection
- `ComboBox` is **editable** (`IsEditable="True"`)
- Users can select from popular languages OR type any custom language
- AI handles translation to any language specified
- `AvailableLanguages` array serves as suggestions, not restrictions

### Localization
- Interface supports: Russian (ru), Ossetian (os), English (en)
- Resource files: `Strings.*.resx`

### Hotkeys
- Global hotkeys for translate selection and show/hide window
- Configurable via settings

## Conventions
- ViewModels use `ObservableProperty` attribute from CommunityToolkit
- Settings stored via `ISettingsStore` interface
- Provider configuration supports multiple AI backends

### Translation Profiles
- Users can select a translation profile to optimize AI prompts for specific content types
- Built-in profiles: General, Technical, Literary, Legal, Medical, Casual
- Profiles stored in `TranslationProfile.cs` with `NameKey` for localization
- Active profile saved in `AppSettings.ActiveProfileId`
- Profile hints are appended to the system prompt in `TranslationService`

### Auto Profile Detection
- Toggle between manual profile selection and automatic detection
- When enabled, AI analyzes text type and applies appropriate translation style
- Setting saved in `AppSettings.UseAutoProfileDetection`
- UI: ToggleButton with sparkle icon hides/shows profile ComboBox

## DI Registration (App.xaml.cs)
All services are registered via `Microsoft.Extensions.DependencyInjection`:
- **Singletons**: `ISettingsStore`, `IProviderClient`, `ITranslationService`, `IHotkeyService`, `IClipboardService`, `MainViewModel`, `MainWindow`
- **Transient**: `SettingsViewModel`, `SettingsWindow`

## Text-to-Speech (TTS) Integration

### Piper TTS Service
- **Backend**: Self-hosted Piper TTS (piper1-gpl)
- **Base URL**: `https://tts.rox-net.ru`
- **Supported languages**: ru, en, de, es, fr, it, hi

### API Contract
```http
POST /{lang}/api/tts
Content-Type: application/json

{ "text": "...", "audio_format": "wav" }
```
Response: raw WAV binary

### Architecture
- `ITtsService` / `PiperTtsService` — HTTP client for Piper API
- `IAudioPlayerService` / `AudioPlayerService` — WAV playback via `System.Media.SoundPlayer`
- `LanguageNormalizer` — maps user language input to 2-letter codes

### Settings
- `AppSettings.TtsEnabled` — enable/disable TTS (default: true)
- `AppSettings.AutoSpeakAfterTranslate` — auto-speak after translation (default: false)

### UI
- Speaker button 🔊 appears when translation is available and language is supported
- Button toggles playback (click again to stop)
- Green color indicates active playback

## Coding Conventions
- Use file-scoped namespaces
- Interfaces in separate `Interfaces/` folders
- ImplicitUsings enabled - avoid redundant `using` statements
- All settings persisted via `ISettingsStore` including `ActiveProfileId` and `UseAutoProfileDetection`

## Translation History
- `ITranslationHistoryService` / `TranslationHistoryService` — manages translation history
- History stored in `%LOCALAPPDATA%\QuickTranslate\history.json`
- Max 100 items, favorites are preserved during cleanup
- `TranslationHistoryItem` model with source/target text, languages, timestamp, favorite flag

## HTTP Client & Retry Policy
- `OpenAiProviderClient` uses `IHttpClientFactory` for proper connection pooling
- Built-in retry logic: 3 attempts with exponential backoff (1s, 2s, 4s)
- Retryable errors: 429, 500, 502, 503, 504, timeout
- Authorization header set per-request (not on HttpClient)

## Settings Caching
- `SettingsStore` caches settings in memory
- Cache invalidated when file modification time changes
- Thread-safe with lock synchronization

## Language Support
- `LanguageNormalizer` supports 15 languages: ru, en, de, es, fr, it, hi, zh, ja, ko, pt, ar, tr, pl, uk
- TTS supported for: ru, en, de, es, fr, it, hi
- `GetLanguageDisplayName()` helper for UI display
