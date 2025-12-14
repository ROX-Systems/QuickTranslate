namespace QuickTranslate.Desktop.Services.Interfaces;

public interface IBrowserService
{
    bool IsBrowserActive();
    Task<string?> GetCurrentUrlAsync();
    string? GetActiveBrowserName();
}
