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
