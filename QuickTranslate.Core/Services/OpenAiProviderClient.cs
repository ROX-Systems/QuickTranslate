using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using Serilog;

namespace QuickTranslate.Core.Services;

public class OpenAiProviderClient : IProviderClient, IDisposable
{
    private HttpClient _httpClient;
    private ProviderConfig _provider;
    private readonly ILogger _logger;

    public ProviderConfig? CurrentProvider => _provider;

    public OpenAiProviderClient(ProviderConfig? provider = null)
    {
        _provider = provider ?? new ProviderConfig();
        _logger = Log.ForContext<OpenAiProviderClient>();
        _httpClient = CreateHttpClient();
    }

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_provider.TimeoutSeconds)
        };
        
        if (!string.IsNullOrEmpty(_provider.ApiKey))
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_provider.ApiKey}");
        }
        
        return client;
    }

    public void UpdateProvider(ProviderConfig provider)
    {
        _provider = provider;
        _httpClient.Dispose();
        _httpClient = CreateHttpClient();
        _logger.Information("Provider updated: {Name}", provider.Name);
    }

    public async Task<TranslationResult> SendTranslationRequestAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_provider.ApiKey))
        {
            return TranslationResult.FromError("API key not configured. Please set up a provider in Settings.");
        }

        try
        {
            var request = new ChatCompletionRequest
            {
                Model = _provider.Model,
                Temperature = _provider.Temperature,
                MaxTokens = _provider.MaxTokens,
                Messages = new List<ChatMessage>
                {
                    new() { Role = "system", Content = systemPrompt },
                    new() { Role = "user", Content = userPrompt }
                }
            };

            var baseUrl = _provider.BaseUrl.TrimEnd('/');
            var endpoint = $"{baseUrl}/chat/completions";

            _logger.Information("Sending translation request to {Endpoint}", endpoint);

            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("API request failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
                return TranslationResult.FromError($"API Error: {response.StatusCode} - {responseBody}");
            }

            var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody);

            if (chatResponse?.Error != null)
            {
                return TranslationResult.FromError($"API Error: {chatResponse.Error.Message}");
            }

            var translatedText = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrEmpty(translatedText))
            {
                return TranslationResult.FromError("Empty response from API");
            }

            _logger.Information("Translation completed successfully");
            return TranslationResult.FromSuccess(translatedText.Trim());
        }
        catch (TaskCanceledException)
        {
            _logger.Warning("Translation request was cancelled");
            return TranslationResult.FromError("Request was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "HTTP request failed");
            return TranslationResult.FromError($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error during translation");
            return TranslationResult.FromError($"Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
