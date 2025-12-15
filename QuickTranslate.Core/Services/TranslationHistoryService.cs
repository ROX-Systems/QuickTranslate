using System.Text.Json;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using Serilog;

namespace QuickTranslate.Core.Services;

public class TranslationHistoryService : ITranslationHistoryService
{
    private readonly string _historyPath;
    private readonly ILogger _logger;
    private readonly object _lock = new();
    private List<TranslationHistoryItem> _history = new();
    private const int MaxHistoryItems = 100;

    public TranslationHistoryService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "QuickTranslate");
        Directory.CreateDirectory(appFolder);
        _historyPath = Path.Combine(appFolder, "history.json");
        _logger = Log.ForContext<TranslationHistoryService>();
        
        LoadHistory();
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                var json = File.ReadAllText(_historyPath);
                _history = JsonSerializer.Deserialize<List<TranslationHistoryItem>>(json) ?? new();
                _logger.Information("Loaded {Count} history items", _history.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load translation history");
            _history = new();
        }
    }

    private void SaveHistory()
    {
        try
        {
            var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyPath, json);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save translation history");
        }
    }

    public IReadOnlyList<TranslationHistoryItem> GetHistory(int limit = 50)
    {
        lock (_lock)
        {
            return _history
                .OrderByDescending(h => h.Timestamp)
                .Take(limit)
                .ToList()
                .AsReadOnly();
        }
    }

    public void Add(TranslationHistoryItem item)
    {
        lock (_lock)
        {
            var existing = _history.FirstOrDefault(h => 
                h.SourceText == item.SourceText && 
                h.TargetLanguage == item.TargetLanguage);
            
            if (existing != null)
            {
                existing.TranslatedText = item.TranslatedText;
                existing.Timestamp = DateTime.UtcNow;
                existing.ProviderName = item.ProviderName;
            }
            else
            {
                _history.Insert(0, item);
                
                if (_history.Count > MaxHistoryItems)
                {
                    var nonFavorites = _history
                        .Where(h => !h.IsFavorite)
                        .OrderByDescending(h => h.Timestamp)
                        .Skip(MaxHistoryItems - _history.Count(h => h.IsFavorite))
                        .ToList();
                    
                    foreach (var toRemove in nonFavorites)
                    {
                        _history.Remove(toRemove);
                    }
                }
            }
            
            SaveHistory();
            _logger.Information("Added translation to history");
        }
    }

    public void Remove(string id)
    {
        lock (_lock)
        {
            var item = _history.FirstOrDefault(h => h.Id == id);
            if (item != null)
            {
                _history.Remove(item);
                SaveHistory();
                _logger.Information("Removed history item {Id}", id);
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            var favorites = _history.Where(h => h.IsFavorite).ToList();
            _history = favorites;
            SaveHistory();
            _logger.Information("Cleared history, kept {Count} favorites", favorites.Count);
        }
    }

    public void ToggleFavorite(string id)
    {
        lock (_lock)
        {
            var item = _history.FirstOrDefault(h => h.Id == id);
            if (item != null)
            {
                item.IsFavorite = !item.IsFavorite;
                SaveHistory();
                _logger.Information("Toggled favorite for {Id}: {IsFavorite}", id, item.IsFavorite);
            }
        }
    }

    public IReadOnlyList<TranslationHistoryItem> GetFavorites()
    {
        lock (_lock)
        {
            return _history
                .Where(h => h.IsFavorite)
                .OrderByDescending(h => h.Timestamp)
                .ToList()
                .AsReadOnly();
        }
    }
}
