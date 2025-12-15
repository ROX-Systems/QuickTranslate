using QuickTranslate.Core.Models;

namespace QuickTranslate.Core.Interfaces;

public interface ITranslationHistoryService
{
    IReadOnlyList<TranslationHistoryItem> GetHistory(int limit = 50);
    void Add(TranslationHistoryItem item);
    void Remove(string id);
    void Clear();
    void ToggleFavorite(string id);
    IReadOnlyList<TranslationHistoryItem> GetFavorites();
}
