using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using QuickTranslate.Desktop.Services;
using QuickTranslate.Desktop.Services.Interfaces;
using Serilog;

namespace QuickTranslate.Desktop.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly ITranslationHistoryService _historyService;
    private readonly IClipboardService _clipboardService;
    private readonly ILogger _logger;

    [ObservableProperty]
    private ObservableCollection<TranslationHistoryItem> _historyItems = new();

    [ObservableProperty]
    private TranslationHistoryItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showFavoritesOnly;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public event EventHandler<TranslationHistoryItem>? ItemSelected;

    public HistoryViewModel(
        ITranslationHistoryService historyService,
        IClipboardService clipboardService)
    {
        _historyService = historyService;
        _clipboardService = clipboardService;
        _logger = Log.ForContext<HistoryViewModel>();

        LoadHistory();
    }

    public void LoadHistory()
    {
        var items = ShowFavoritesOnly 
            ? _historyService.GetFavorites() 
            : _historyService.GetHistory();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            items = items.Where(i => 
                i.SourceText.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.TranslatedText.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        HistoryItems = new ObservableCollection<TranslationHistoryItem>(items);
        StatusMessage = string.Format(LocalizationService.Instance["HistoryItemsCount"], HistoryItems.Count);
    }

    partial void OnSearchTextChanged(string value)
    {
        LoadHistory();
    }

    partial void OnShowFavoritesOnlyChanged(bool value)
    {
        LoadHistory();
    }

    [RelayCommand]
    private void ToggleFavorite(TranslationHistoryItem? item)
    {
        if (item == null) return;

        _historyService.ToggleFavorite(item.Id);
        item.IsFavorite = !item.IsFavorite;
        
        if (ShowFavoritesOnly)
        {
            LoadHistory();
        }
        
        _logger.Information("Toggled favorite for item {Id}", item.Id);
    }

    [RelayCommand]
    private void DeleteItem(TranslationHistoryItem? item)
    {
        if (item == null) return;

        _historyService.Remove(item.Id);
        HistoryItems.Remove(item);
        StatusMessage = LocalizationService.Instance["HistoryItemDeleted"];
        _logger.Information("Deleted history item {Id}", item.Id);
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _historyService.Clear();
        LoadHistory();
        StatusMessage = LocalizationService.Instance["HistoryCleared"];
        _logger.Information("History cleared");
    }

    [RelayCommand]
    private void CopySource(TranslationHistoryItem? item)
    {
        if (item == null) return;
        _clipboardService.SetText(item.SourceText);
        StatusMessage = LocalizationService.Instance["CopiedToClipboard"];
    }

    [RelayCommand]
    private void CopyTranslation(TranslationHistoryItem? item)
    {
        if (item == null) return;
        _clipboardService.SetText(item.TranslatedText);
        StatusMessage = LocalizationService.Instance["CopiedToClipboard"];
    }

    [RelayCommand]
    private void UseItem(TranslationHistoryItem? item)
    {
        if (item == null) return;
        ItemSelected?.Invoke(this, item);
    }

    public string FormatTimestamp(DateTime timestamp)
    {
        var local = timestamp.ToLocalTime();
        var now = DateTime.Now;
        
        if (local.Date == now.Date)
            return local.ToString("HH:mm");
        
        if (local.Date == now.Date.AddDays(-1))
            return $"{LocalizationService.Instance["Yesterday"]} {local:HH:mm}";
        
        if (local.Year == now.Year)
            return local.ToString("dd MMM HH:mm");
        
        return local.ToString("dd.MM.yyyy HH:mm");
    }
}
