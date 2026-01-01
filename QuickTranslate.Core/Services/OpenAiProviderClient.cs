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

public class OpenAiProviderClient : IProviderClient, IDisposable
{
    private readonly IHttpClientFactory? _httpClientFactory;
    private HttpClient? _ownedHttpClient;
    private ProviderConfig _provider;
    private readonly ILogger _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public ProviderConfig? CurrentProvider => _provider;

    public OpenAiProviderClient(ProviderConfig? provider = null, IHttpClientFactory? httpClientFactory = null)
    {
        _provider = provider ?? new ProviderConfig();
        _httpClientFactory = httpClientFactory;
        _logger = Log.ForContext<OpenAiProviderClient>();

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
            var client = _httpClientFactory.CreateClient("OpenAI");
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

        return await ExecuteWithRetryAsync(endpoint, request, cancellationToken);
    }

    private async Task<TranslationResult> ExecuteWithRetryAsync(
        string endpoint,
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await SendRequestAsync(endpoint, request, cancellationToken);
            return result;
        }
        catch (TaskCanceledException)
        {
            _logger.Warning("Translation request was cancelled");
            return TranslationResult.FromError("Request was cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Translation failed after all retry attempts");
            return TranslationResult.FromError($"Error: {ex.Message}");
        }
    }

    private async Task<TranslationResult> SendRequestAsync(
        string endpoint,
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var httpClient = GetHttpClient();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Add("Authorization", $"Bearer {_provider.ApiKey}");

        var jsonContent = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.Debug("Sending translation request to {BaseUrl}", _provider.BaseUrl);

        HttpResponseMessage response = await _retryPolicy.ExecuteAsync(async ct =>
        {
            return await httpClient.SendAsync(httpRequest, ct);
        }, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.Error("API request failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
            return TranslationResult.FromError($"API Error: {response.StatusCode} - {responseBody}");
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

        var translatedText = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrEmpty(translatedText))
        {
            _logger.Warning("API response contained no translated text");
            return TranslationResult.FromError("Empty response from API");
        }

        _logger.Debug("Translation completed successfully");
        return TranslationResult.FromSuccess(translatedText.Trim());
    }

    public void Dispose()
    {
        _ownedHttpClient?.Dispose();
    }
}
