namespace QuickTranslate.Core.Models;

public class TranslationHistoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string SourceText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public string? ProfileId { get; set; }
    public bool IsFavorite { get; set; }
}
