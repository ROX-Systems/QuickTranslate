# QuickTranslate

Windows desktop application for quick text translation using OpenAI-compatible APIs.

## Features

- **Global Hotkeys** (work even when app is not focused):
  - `Ctrl+Shift+T` — Translate selected text from any application
  - `Ctrl+Shift+P` — Translate current browser page
  - `Ctrl+Shift+O` — Show/Hide main window

- **Multiple AI Providers**:
  - Add unlimited providers (OpenAI, Azure, local LLMs, etc.)
  - Quick switch between providers from main window
  - Each provider has independent settings (API key, model, parameters)
  - Duplicate providers for easy configuration

- **Smart Translation**:
  - Auto-detects source language
  - Supports multiple target languages (Russian, English, German, French, Spanish, Chinese, Japanese, Korean)
  - Works with any OpenAI-compatible API

- **Browser Page Translation**:
  - Automatically extracts URL from active browser (Chrome, Edge, Firefox, etc.)
  - Downloads and parses HTML to extract readable text
  - Handles large pages with automatic truncation

- **Security**:
  - API key encrypted with Windows DPAPI
  - Stored securely in user profile

## Requirements

- Windows 10/11
- .NET 8.0 SDK

## Build & Run

```bash
# Clone or navigate to project directory
cd QuickTranslate

# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run --project QuickTranslate.Desktop
```

Or build release version:
```bash
dotnet publish QuickTranslate.Desktop -c Release -r win-x64 --self-contained false
```

## Configuration

### Adding AI Providers

1. Launch the application
2. Click **Settings** button (or access via tray icon)
3. In the left panel, click **+ Add** to create a new provider
4. Configure provider settings:
   - **Provider Name**: Display name (e.g., "OpenAI GPT-4", "Local Ollama")
   - **Base URL**: API endpoint (e.g., `https://api.openai.com/v1`)
   - **API Key**: Your API key for this provider
   - **Model**: Model name (e.g., `gpt-4o-mini`, `gpt-4o`, `gpt-3.5-turbo`)
   - **Temperature**: Creativity (0-2, default: 0.3)
   - **Max Tokens**: Maximum response length
   - **Timeout**: Request timeout in seconds

5. Click **Test Connection** to verify settings
6. Click **Save All** to save all providers

### Managing Providers

- **+ Add**: Create a new provider
- **⧉ Copy**: Duplicate selected provider (useful for similar configurations)
- **✕ Delete**: Remove selected provider

### Switching Providers

Select the active provider from the dropdown in the main window header. The selected provider is automatically saved and persists across sessions.

### Settings Location

Settings are stored in:
```
%LOCALAPPDATA%\QuickTranslate\settings.json
```

API key is encrypted using Windows DPAPI and cannot be read on other machines.

### Logs Location

Logs are stored in:
```
%LOCALAPPDATA%\QuickTranslate\logs\
```

## Usage

### Translate Selected Text
1. Select text in any application (browser, Word, Telegram, etc.)
2. Press `Ctrl+Shift+T`
3. QuickTranslate window appears with translation

### Translate Browser Page
1. Open a page in Chrome, Edge, or Firefox
2. Press `Ctrl+Shift+P`
3. QuickTranslate extracts page content and translates it

### Manual Translation
1. Open QuickTranslate (`Ctrl+Shift+O`)
2. Paste or type text in "Source Text" field
3. Click **Translate**

## Architecture

```
QuickTranslate/
├── QuickTranslate.Core/          # Core business logic
│   ├── Models/                   # Data models
│   ├── Interfaces/               # Service interfaces
│   └── Services/                 # Service implementations
│       ├── OpenAiProviderClient  # HTTP client for OpenAI API
│       ├── TranslationService    # Translation orchestration
│       ├── SettingsStore         # Settings persistence with DPAPI
│       └── HtmlExtractor         # HTML parsing
│
└── QuickTranslate.Desktop/       # WPF application
    ├── Services/                 # Desktop-specific services
    │   ├── HotkeyService         # Global hotkeys (Win32 API)
    │   ├── ClipboardService      # Clipboard operations
    │   └── BrowserService        # Browser URL extraction
    ├── ViewModels/               # MVVM ViewModels
    ├── Views/                    # WPF Windows
    └── Converters/               # Value converters
```

## Supported OpenAI-Compatible APIs

- OpenAI (api.openai.com)
- Azure OpenAI
- Anthropic (via compatible proxy)
- Local LLMs (LM Studio, Ollama with OpenAI compatibility layer)
- Any service implementing `/v1/chat/completions` endpoint

## Design Decisions

1. **WPF over WinUI 3**: Better stability and ecosystem for system tray integration
2. **DPAPI for API key**: Windows-native encryption, no additional dependencies
3. **Clipboard simulation**: Universal method to get selected text across all apps
4. **HtmlAgilityPack**: Mature HTML parsing library with good performance
5. **CommunityToolkit.Mvvm**: Modern MVVM with source generators
6. **Serilog**: Structured logging with file sink

## Known Limitations

- Page translation works only with Chromium-based browsers and Firefox
- Some websites may block HTML fetching (CORS/auth requirements)
- Very long texts may be truncated to fit API limits

## Troubleshooting

### Hotkeys not working
- Check if another application is using the same hotkeys
- Run as Administrator if needed
- Check logs for registration errors

### Translation fails
- Verify API key in Settings
- Check Base URL format (should end without `/`)
- Test connection in Settings
- Check logs for detailed error messages

### Clipboard issues
- Some applications may not support standard copy (Ctrl+C)
- Try selecting text manually and using the app's Translate button

## License

MIT License
