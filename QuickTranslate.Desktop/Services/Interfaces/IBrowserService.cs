namespace QuickTranslate.Desktop.Services.Interfaces;

public interface IBrowserService
{
    bool IsBrowserActive(IntPtr? targetWindow = null);
    Task<string?> GetCurrentUrlAsync(IntPtr? targetWindow = null);
    string? GetActiveBrowserName(IntPtr? targetWindow = null);
}
