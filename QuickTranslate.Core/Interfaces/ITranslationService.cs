using QuickTranslate.Core.Models;

namespace QuickTranslate.Core.Interfaces;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default);
}
