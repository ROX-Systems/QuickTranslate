namespace QuickTranslate.Desktop.Services.Interfaces;

public interface IClipboardService
{
    Task<string?> GetSelectedTextAsync(IntPtr? targetWindow = null);
    void SetText(string text);
    string? GetText();
}
