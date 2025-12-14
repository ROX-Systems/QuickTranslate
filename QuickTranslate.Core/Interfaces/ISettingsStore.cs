using QuickTranslate.Core.Models;

namespace QuickTranslate.Core.Interfaces;

public interface ISettingsStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}
