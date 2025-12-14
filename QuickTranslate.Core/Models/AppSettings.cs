namespace QuickTranslate.Core.Models;

public class AppSettings
{
    public List<ProviderConfig> Providers { get; set; } = new();
    public string? ActiveProviderId { get; set; }
    public string TargetLanguage { get; set; } = "Russian";

    public ProviderConfig? GetActiveProvider()
    {
        if (string.IsNullOrEmpty(ActiveProviderId))
            return Providers.FirstOrDefault();
        
        return Providers.FirstOrDefault(p => p.Id == ActiveProviderId) 
               ?? Providers.FirstOrDefault();
    }

    public void SetActiveProvider(string providerId)
    {
        if (Providers.Any(p => p.Id == providerId))
        {
            ActiveProviderId = providerId;
        }
    }
}
