namespace QuickTranslate.Desktop.Services.Interfaces;

public interface IClipboardService
{
    Task<string?> GetSelectedTextAsync();
    void SetText(string text);
    string? GetText();
}
