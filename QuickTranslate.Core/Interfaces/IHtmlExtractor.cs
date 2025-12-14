namespace QuickTranslate.Core.Interfaces;

public interface IHtmlExtractor
{
    Task<string> ExtractTextFromUrlAsync(string url, CancellationToken cancellationToken = default);
    string ExtractTextFromHtml(string html);
}
