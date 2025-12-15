using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuickTranslate.Core.Interfaces;
using Serilog;

namespace QuickTranslate.Core.Services;

public class PiperTtsService : ITtsService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly string _baseUrl;

    private static readonly Dictionary<string, string> TtsEndpoints = new()
    {
        ["ru"] = "https://tts.rox-net.ru/ru/api/tts",
        ["en"] = "https://tts.rox-net.ru/en/api/tts",
        ["de"] = "https://tts.rox-net.ru/de/api/tts",
        ["es"] = "https://tts.rox-net.ru/es/api/tts",
        ["fr"] = "https://tts.rox-net.ru/fr/api/tts",
        ["it"] = "https://tts.rox-net.ru/it/api/tts",
        ["hi"] = "https://tts.rox-net.ru/hi/api/tts"
    };

    public PiperTtsService(string? baseUrl = null)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _logger = Log.ForContext<PiperTtsService>();
        _baseUrl = baseUrl ?? "https://tts.rox-net.ru";
    }

    public async Task<byte[]?> SynthesizeAsync(string text, string languageCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.Warning("TTS: Empty text provided");
            return null;
        }

        var normalizedLang = LanguageNormalizer.Normalize(languageCode);
        
        if (!TtsEndpoints.TryGetValue(normalizedLang, out var endpoint))
        {
            _logger.Warning("TTS: Unsupported language {Language}, falling back to ru", languageCode);
            endpoint = TtsEndpoints["ru"];
            normalizedLang = "ru";
        }

        _logger.Information("TTS: Synthesizing {Length} chars in {Language}", text.Length, normalizedLang);

        try
        {
            var request = new TtsRequest { Text = text, AudioFormat = "wav" };
            
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("TTS: Request failed with status {Status}", response.StatusCode);
                return null;
            }

            var audioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            _logger.Information("TTS: Received {Size} bytes of audio", audioData.Length);
            
            return audioData;
        }
        catch (TaskCanceledException)
        {
            _logger.Information("TTS: Request cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "TTS: Synthesis failed");
            return null;
        }
    }

    public bool IsLanguageSupported(string languageCode)
    {
        return LanguageNormalizer.IsTtsSupported(languageCode);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private class TtsRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        
        [JsonPropertyName("audio_format")]
        public string AudioFormat { get; set; } = "wav";
    }
}
