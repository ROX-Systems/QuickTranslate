namespace QuickTranslate.Core.Models;

public class TranslationRequest
{
    public string SourceText { get; set; } = string.Empty;
    public string? SourceLanguage { get; set; }
    public string TargetLanguage { get; set; } = "Russian";
}
