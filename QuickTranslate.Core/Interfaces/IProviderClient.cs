using QuickTranslate.Core.Models;

namespace QuickTranslate.Core.Interfaces;

/// <summary>
/// Provides methods for sending translation requests to AI providers.
/// </summary>
public interface IProviderClient
{
    /// <summary>
    /// Sends a translation request to the configured AI provider.
    /// </summary>
    /// <param name="systemPrompt">The system prompt defining the assistant's behavior.</param>
    /// <param name="userPrompt">The user prompt containing the text to translate.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the request.</param>
    /// <returns>A task containing the translation result.</returns>
    Task<TranslationResult> SendTranslationRequestAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the provider configuration with new settings.
    /// </summary>
    /// <param name="provider">The new provider configuration.</param>
    void UpdateProvider(ProviderConfig provider);

    /// <summary>
    /// Gets the currently configured provider.
    /// </summary>
    ProviderConfig? CurrentProvider { get; }
}
