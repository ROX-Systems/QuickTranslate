namespace QuickTranslate.Core.Interfaces;

public interface ITtsService
{
    Task<byte[]?> SynthesizeAsync(string text, string languageCode, CancellationToken cancellationToken = default);
    bool IsLanguageSupported(string languageCode);
}
