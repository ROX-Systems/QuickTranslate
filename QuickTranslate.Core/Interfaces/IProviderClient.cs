using QuickTranslate.Core.Models;

namespace QuickTranslate.Core.Interfaces;

public interface IProviderClient
{
    Task<TranslationResult> SendTranslationRequestAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
    
    void UpdateProvider(ProviderConfig provider);
    ProviderConfig? CurrentProvider { get; }
}
