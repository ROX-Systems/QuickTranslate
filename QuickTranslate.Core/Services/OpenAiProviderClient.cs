using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan[] RetryDelays = { 
        TimeSpan.FromSeconds(1), 
        TimeSpan.FromSeconds(2), 
        TimeSpan.FromSeconds(4) 
    };

    public ProviderConfig? CurrentProvider => _provider;

    public OpenAiProviderClient(ProviderConfig? provider = null, IHttpClientFactory? httpClientFactory = null)
    {
        _provider = provider ?? new ProviderConfig();
        _httpClientFactory = httpClientFactory;
        _logger = Log.ForContext<OpenAiProviderClient>();
        
        if (_httpClientFactory == null)
        {
            _ownedHttpClient = CreateHttpClient();
        }
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
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= MaxRetryAttempts; attempt++)
        {
            if (attempt > 0)
            {
                var delay = RetryDelays[Math.Min(attempt - 1, RetryDelays.Length - 1)];
                _logger.Information("Retry attempt {Attempt} after {Delay}s", attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }

            try
            {
                var result = await SendRequestAsync(endpoint, request, cancellationToken);
                
                if (result.Success || !IsRetryableError(result.ErrorMessage))
                {
                    return result;
                }
                
                _logger.Warning("Retryable error on attempt {Attempt}: {Error}", attempt + 1, result.ErrorMessage);
            }
            catch (TaskCanceledException)
            {
                _logger.Warning("Translation request was cancelled");
                return TranslationResult.FromError("Request was cancelled");
            }
            catch (HttpRequestException ex) when (IsRetryableException(ex))
            {
                lastException = ex;
                _logger.Warning(ex, "Retryable HTTP error on attempt {Attempt}", attempt + 1);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Non-retryable error during translation");
                return TranslationResult.FromError($"Error: {ex.Message}");
            }
        }

        _logger.Error(lastException, "All retry attempts exhausted");
        return TranslationResult.FromError($"Network error after {MaxRetryAttempts} retries: {lastException?.Message}");
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

        _logger.Information("Sending translation request to {Endpoint}", endpoint);

        var response = await httpClient.SendAsync(httpRequest, cancellationToken);
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

    private static bool IsRetryableError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage)) return false;
        
        return errorMessage.Contains("429") || 
               errorMessage.Contains("500") || 
               errorMessage.Contains("502") || 
               errorMessage.Contains("503") || 
               errorMessage.Contains("504") ||
               errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRetryableException(HttpRequestException ex)
    {
        return ex.StatusCode is 
            System.Net.HttpStatusCode.TooManyRequests or
            System.Net.HttpStatusCode.InternalServerError or
            System.Net.HttpStatusCode.BadGateway or
            System.Net.HttpStatusCode.ServiceUnavailable or
            System.Net.HttpStatusCode.GatewayTimeout;
    }

    public void Dispose()
    {
        _ownedHttpClient?.Dispose();
    }
}
