using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Retry;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using Serilog;

namespace QuickTranslate.Core.Services;

/// <summary>
/// Universal client for OpenAI-compatible providers
/// Supports: OpenAI, z.ai, Groq, Together, Ollama, and other OpenAI-compatible APIs
/// </summary>
public class OpenAiCompatibleProviderClient : IProviderClient, IDisposable
{
    private readonly IHttpClientFactory? _httpClientFactory;
    private HttpClient? _ownedHttpClient;
    private ProviderConfig _provider;
    private readonly ILogger _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public ProviderConfig? CurrentProvider => _provider;

    public OpenAiCompatibleProviderClient(ProviderConfig? provider = null, IHttpClientFactory? httpClientFactory = null)
    {
        _provider = provider ?? new ProviderConfig();
        _httpClientFactory = httpClientFactory;
        _logger = Log.ForContext<OpenAiCompatibleProviderClient>();

        _retryPolicy = CreateRetryPolicy();

        if (_httpClientFactory == null)
        {
            _ownedHttpClient = CreateHttpClient();
        }
    }

    private AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r =>
                !r.IsSuccessStatusCode && IsRetryableStatusCode(r.StatusCode))
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger.Warning(
                        outcome.Exception,
                        "Retry {RetryAttempt} after {Delay}s due to: {Reason}",
                        retryAttempt,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    private static bool IsRetryableStatusCode(System.Net.HttpStatusCode statusCode)
    {
        return statusCode is
            System.Net.HttpStatusCode.TooManyRequests or
            System.Net.HttpStatusCode.InternalServerError or
            System.Net.HttpStatusCode.BadGateway or
            System.Net.HttpStatusCode.ServiceUnavailable or
            System.Net.HttpStatusCode.GatewayTimeout;
    }

    private HttpClient GetHttpClient()
    {
        if (_httpClientFactory != null)
        {
            // Create a client without naming to avoid hardcoded configurations
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(_provider.TimeoutSeconds);
            return client;
        }

        return _ownedHttpClient ?? throw new InvalidOperationException("HttpClient not initialized");
    }

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_provider.TimeoutSeconds)
        };
        return client;
    }

    public void UpdateProvider(ProviderConfig provider)
    {
        _provider = provider;

        if (_httpClientFactory == null && _ownedHttpClient != null)
        {
            _ownedHttpClient.Dispose();
            _ownedHttpClient = CreateHttpClient();
        }

        _logger.Information("Provider updated: {Name} ({Type})", provider.Name, provider.Type);
    }

    public async Task<TranslationResult> SendTranslationRequestAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_provider.ApiKey))
            {
                return TranslationResult.FromError("API key not configured. Please set up a provider in Settings.");
            }

            var request = BuildChatCompletionRequest(systemPrompt, userPrompt);
            var endpoint = BuildEndpoint();

            return await SendRequestAsync(endpoint, request, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _logger.Warning("Translation request was cancelled");
            return TranslationResult.FromError("Request was cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Translation failed");
            return TranslationResult.FromError($"Error: {ex.Message}");
        }
    }

    private ChatCompletionRequest BuildChatCompletionRequest(string systemPrompt, string userPrompt)
    {
        return new ChatCompletionRequest
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
    }

    private string BuildEndpoint()
    {
        var baseUrl = _provider.BaseUrl.TrimEnd('/');

        // If URL already ends with the full endpoint, use it as-is
        if (baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return baseUrl;
        }

        // For OpenAI-compatible APIs, append /chat/completions
        return $"{baseUrl}/chat/completions";
    }

    private async Task<TranslationResult> SendRequestAsync(
        string endpoint,
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var httpClient = GetHttpClient();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        AddAuthorizationHeader(httpRequest);

        var jsonContent = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.Debug("Sending translation request to {Endpoint} for model {Model}", endpoint, _provider.Model);
        _logger.Debug("Request payload: {Json}", jsonContent);

        HttpResponseMessage response = await _retryPolicy.ExecuteAsync(async ct =>
        {
            return await httpClient.SendAsync(httpRequest, ct);
        }, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.Debug("API response status: {StatusCode}", response.StatusCode);
        _logger.Debug("API response length: {Length} chars", responseBody?.Length ?? 0);
        _logger.Debug("Raw API response: {Body}", responseBody);

        if (!response.IsSuccessStatusCode)
        {
            _logger.Error("API request failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
            return TranslationResult.FromError($"API Error ({response.StatusCode}): {GetErrorMessage(responseBody)}");
        }

        var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody);

        if (chatResponse == null)
        {
            _logger.Error("Failed to deserialize API response: {Body}", responseBody);
            return TranslationResult.FromError("Invalid response format from API");
        }

        if (chatResponse.Error != null)
        {
            var errorType = chatResponse.Error.Type ?? "Unknown";
            var errorMessage = $"API Error ({errorType}): {chatResponse.Error.Message}";
            _logger.Error("API returned error: {Type} - {Message}", errorType, chatResponse.Error.Message);
            return TranslationResult.FromError(errorMessage);
        }

        var choices = chatResponse.Choices ?? chatResponse.Data;
        var translatedText = choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrEmpty(translatedText))
        {
            _logger.Warning("API response contained no translated text");
            _logger.Warning("Choices (via 'choices' field): {Count}", chatResponse.Choices?.Count ?? 0);
            _logger.Warning("Data (via 'data' field): {Count}", chatResponse.Data?.Count ?? 0);
            _logger.Warning("Raw API response: {Body}", responseBody);
            return TranslationResult.FromError("Empty response from API");
        }

        _logger.Debug("Translation completed successfully");
        return TranslationResult.FromSuccess(translatedText.Trim());
    }

    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        // OpenAI and compatible providers use Bearer token
        if (_provider.Type == ProviderType.OpenAI || _provider.Type == ProviderType.Ollama)
        {
            // Ollama doesn't require authentication by default
            if (!string.IsNullOrEmpty(_provider.ApiKey))
            {
                request.Headers.Add("Authorization", $"Bearer {_provider.ApiKey}");
            }
        }
        // Future support for other providers
        // Anthropic, Google, etc. can be added here
    }

    private static string GetErrorMessage(string responseBody)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(responseBody);
            if (jsonDoc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? responseBody;
                }
            }
            return responseBody;
        }
        catch
        {
            return responseBody;
        }
    }

    public void Dispose()
    {
        _ownedHttpClient?.Dispose();
    }
}
