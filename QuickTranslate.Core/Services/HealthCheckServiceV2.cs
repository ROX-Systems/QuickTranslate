using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http;
using QuickTranslate.Core.Factories;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using QuickTranslate.Core.Services;
using Serilog;

namespace QuickTranslate.Core.Services;

/// <summary>
/// Health check service using provider clients for testing connectivity
/// </summary>
public class HealthCheckServiceV2 : IHealthCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public HealthCheckServiceV2(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = Log.ForContext<HealthCheckServiceV2>();
    }

    public async Task<HealthCheckResult> CheckProviderHealthAsync(ProviderConfig provider, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(provider.ApiKey))
            {
                // Ollama might not require API key
                if (provider.Type != ProviderType.Ollama)
                {
                    return HealthCheckResult.Unhealthy("API key is not configured");
                }
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(Math.Min(provider.TimeoutSeconds, 30));

            var baseUrl = provider.BaseUrl.TrimEnd('/');
            var endpoint = BuildHealthCheckEndpoint(baseUrl, provider.Type);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            AddAuthHeader(httpRequest, provider);

            var requestPayload = BuildHealthCheckRequest(provider);
            var jsonContent = JsonSerializer.Serialize(requestPayload);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.Debug("Sending health check to {Endpoint} for provider {Provider}", endpoint, provider.Name);

            var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"API returned status {response.StatusCode}";
                _logger.Warning("Provider health check failed for {Provider}: {Error}", provider.Name, errorMessage);
                return HealthCheckResult.Unhealthy(errorMessage);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!string.IsNullOrEmpty(responseBody))
            {
                // Try to check for errors in response
                var hasError = CheckResponseForErrors(responseBody);
                if (hasError.hasError)
                {
                    _logger.Warning("Provider health check failed for {Provider}: {Error}", provider.Name, hasError.errorMessage);
                    return HealthCheckResult.Unhealthy(hasError.errorMessage);
                }

                _logger.Debug("Provider health check passed for {Provider}", provider.Name);
                return HealthCheckResult.Healthy($"Provider '{provider.Name}' is responding normally");
            }

            _logger.Warning("Provider health check failed for {Provider}: Empty response", provider.Name);
            return HealthCheckResult.Unhealthy("Empty response from API");
        }
        catch (TaskCanceledException)
        {
            _logger.Warning("Provider health check timed out for {Provider}", provider.Name);
            return HealthCheckResult.Unhealthy("Request timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "Provider health check failed for {Provider}: Network error", provider.Name);
            return HealthCheckResult.Unhealthy($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Provider health check failed for {Provider}: Unexpected error", provider.Name);
            return HealthCheckResult.Unhealthy($"Unexpected error: {ex.Message}");
        }
    }

    private static string BuildHealthCheckEndpoint(string baseUrl, ProviderType providerType)
    {
        // If URL already ends with the full endpoint, use it as-is
        if (baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return baseUrl;
        }

        return $"{baseUrl}/chat/completions";
    }

    private static void AddAuthHeader(HttpRequestMessage request, ProviderConfig provider)
    {
        if (provider.Type == ProviderType.Ollama && string.IsNullOrEmpty(provider.ApiKey))
        {
            // Ollama might not require auth
            return;
        }

        if (!string.IsNullOrEmpty(provider.ApiKey))
        {
            request.Headers.Add("Authorization", $"Bearer {provider.ApiKey}");
        }
    }

    private static object BuildHealthCheckRequest(ProviderConfig provider)
    {
        return new
        {
            model = provider.Model,
            temperature = 0,
            max_tokens = 10,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are a health check assistant."
                },
                new
                {
                    role = "user",
                    content = "Respond with exactly 'OK' and nothing else."
                }
            }
        };
    }

    private static (bool hasError, string? errorMessage) CheckResponseForErrors(string responseBody)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(responseBody);

            // Check for OpenAI-compatible error format
            if (jsonDoc.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.TryGetProperty("message", out var message))
                {
                    return (true, $"API Error: {message.GetString()}");
                }
                return (true, $"API Error: {errorElement.ToString()}");
            }

            return (false, null);
        }
        catch
        {
            // If we can't parse the response, assume it's OK
            return (false, null);
        }
    }

    public async Task<HealthCheckResult> CheckTtsHealthAsync(string? ttsEndpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = ttsEndpoint ?? "https://tts.rox-net.ru";
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var healthCheckUrl = $"{endpoint}/ru/api/health";

            var response = await httpClient.GetAsync(healthCheckUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.Debug("TTS health check passed");
                return HealthCheckResult.Healthy("TTS service is available");
            }

            var errorMessage = $"TTS service returned status {response.StatusCode}";
            _logger.Warning("TTS health check failed: {Error}", errorMessage);
            return HealthCheckResult.Unhealthy(errorMessage);
        }
        catch (TaskCanceledException)
        {
            _logger.Warning("TTS health check timed out");
            return HealthCheckResult.Unhealthy("TTS service request timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "TTS health check failed: Network error");
            return HealthCheckResult.Unhealthy($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "TTS health check failed: Unexpected error");
            return HealthCheckResult.Unhealthy($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Dictionary<string, HealthCheckResult>> CheckAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, HealthCheckResult>();

        await Task.WhenAll(
            Task.Run(async () =>
            {
                var result = await CheckTtsHealthAsync(null, cancellationToken);
                results["TTS"] = result;
            }, cancellationToken)
        );

        return results;
    }
}
