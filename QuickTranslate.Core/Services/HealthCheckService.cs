using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using Serilog;

namespace QuickTranslate.Core.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public HealthCheckService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = Log.ForContext<HealthCheckService>();
    }

    public async Task<HealthCheckResult> CheckProviderHealthAsync(ProviderConfig provider, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(provider.ApiKey))
            {
                return HealthCheckResult.Unhealthy("API key is not configured");
            }

            var httpClient = _httpClientFactory.CreateClient("OpenAI");
            httpClient.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds);

            var baseUrl = provider.BaseUrl.TrimEnd('/');
            var endpoint = $"{baseUrl}/chat/completions";

            var request = new ChatCompletionRequest
            {
                Model = provider.Model,
                Temperature = 0,
                MaxTokens = 10,
                Messages = new List<ChatMessage>
                {
                    new() { Role = "system", Content = "You are a health check assistant." },
                    new() { Role = "user", Content = "Respond with exactly 'OK' and nothing else." }
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Add("Authorization", $"Bearer {provider.ApiKey}");
            var jsonContent = JsonSerializer.Serialize(request);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"API returned status {response.StatusCode}";
                _logger.Warning("Provider health check failed for {Provider}: {Error}", provider.Name, errorMessage);
                return HealthCheckResult.Unhealthy(errorMessage);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody);

            if (chatResponse?.Error != null)
            {
                var errorMessage = $"API Error: {chatResponse.Error.Message}";
                _logger.Warning("Provider health check failed for {Provider}: {Error}", provider.Name, errorMessage);
                return HealthCheckResult.Unhealthy(errorMessage);
            }

            var translatedText = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

            if (!string.IsNullOrEmpty(translatedText))
            {
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
