using QuickTranslate.Core.Models;

namespace QuickTranslate.Desktop.ViewModels;

public class ProviderTypeInfo
{
    public ProviderType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
