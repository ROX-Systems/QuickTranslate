using QuickTranslate.Core.Models;

namespace QuickTranslate.Core.Interfaces;

/// <summary>
/// Provides high-level translation orchestration including prompt building.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translates text according to the provided request parameters.
    /// </summary>
    /// <param name="request">The translation request containing source text, language, and profile.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the translation.</param>
    /// <returns>A task containing the translation result.</returns>
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default);
}
