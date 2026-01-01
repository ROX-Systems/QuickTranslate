using Microsoft.Extensions.Http;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using QuickTranslate.Core.Services;

namespace QuickTranslate.Core.Factories;

/// <summary>
/// Factory for creating provider clients based on provider type
/// </summary>
public static class ProviderClientFactory
{
    /// <summary>
    /// Create a provider client instance based on the provider configuration
    /// </summary>
    public static IProviderClient CreateClient(
        ProviderConfig? provider,
        IHttpClientFactory? httpClientFactory = null)
    {
        if (provider == null)
        {
            return new OpenAiCompatibleProviderClient(null, httpClientFactory);
        }

        return provider.Type switch
        {
            ProviderType.OpenAI => new OpenAiCompatibleProviderClient(provider, httpClientFactory),
            ProviderType.Anthropic => new OpenAiCompatibleProviderClient(provider, httpClientFactory),
            ProviderType.Google => new OpenAiCompatibleProviderClient(provider, httpClientFactory),
            ProviderType.Ollama => new OpenAiCompatibleProviderClient(provider, httpClientFactory),
            ProviderType.Custom => new OpenAiCompatibleProviderClient(provider, httpClientFactory),
            _ => new OpenAiCompatibleProviderClient(provider, httpClientFactory)
        };
    }
}
