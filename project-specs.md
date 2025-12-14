# QuickTranslate - Project Specifications

## Overview
Desktop translation application using AI providers (OpenAI-compatible APIs).

## Tech Stack
- **Framework**: WPF (.NET)
- **UI Library**: WPF-UI (Fluent Design)
- **MVVM**: CommunityToolkit.Mvvm
- **Logging**: Serilog

## Architecture
```
QuickTranslate.sln
├── QuickTranslate.Core/       # Business logic, models, interfaces
│   ├── Interfaces/
│   ├── Models/
│   └── Services/
└── QuickTranslate.Desktop/    # WPF application
    ├── ViewModels/
    ├── Views/
    ├── Services/
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
