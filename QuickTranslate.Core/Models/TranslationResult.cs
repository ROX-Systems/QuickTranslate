namespace QuickTranslate.Core.Models;

public class TranslationResult
{
    public bool Success { get; set; }
    public string TranslatedText { get; set; } = string.Empty;
    public string? DetectedLanguage { get; set; }
    public string? ErrorMessage { get; set; }

    public static TranslationResult FromError(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };

    public static TranslationResult FromSuccess(string translated, string? detectedLanguage = null) => new()
    {
        Success = true,
        TranslatedText = translated,
        DetectedLanguage = detectedLanguage
    };
}
