using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using QuickTranslate.Core.Interfaces;
using Serilog;

namespace QuickTranslate.Core.Services;

public class HtmlExtractor : IHtmlExtractor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public HtmlExtractor()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _logger = Log.ForContext<HtmlExtractor>();
    }

    public async Task<string> ExtractTextFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Information("Fetching URL: {Url}", url);
            var html = await _httpClient.GetStringAsync(url, cancellationToken);
            return ExtractTextFromHtml(html);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to fetch URL: {Url}", url);
            throw;
        }
    }

    public string ExtractTextFromHtml(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            RemoveUnwantedNodes(doc);

            var mainContent = FindMainContent(doc);
            var text = ExtractText(mainContent ?? doc.DocumentNode);

            text = CleanText(text);

            _logger.Information("Extracted {Length} characters from HTML", text.Length);
            return text;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to extract text from HTML");
            throw;
        }
    }

    private void RemoveUnwantedNodes(HtmlDocument doc)
    {
        var nodesToRemove = new[] { "script", "style", "nav", "footer", "header", "aside", "noscript", "iframe", "svg" };
        
        foreach (var tagName in nodesToRemove)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//{tagName}");
            if (nodes != null)
            {
                foreach (var node in nodes.ToList())
                {
                    node.Remove();
                }
            }
        }

        var hiddenNodes = doc.DocumentNode.SelectNodes("//*[@style[contains(., 'display:none') or contains(., 'display: none')]]");
        if (hiddenNodes != null)
        {
            foreach (var node in hiddenNodes.ToList())
            {
                node.Remove();
            }
        }
    }

    private HtmlNode? FindMainContent(HtmlDocument doc)
    {
        var contentSelectors = new[]
        {
            "//article",
            "//main",
            "//*[@id='content']",
            "//*[@id='main-content']",
            "//*[@class='content']",
            "//*[@class='post-content']",
            "//*[@class='article-content']",
            "//*[@role='main']"
        };

        foreach (var selector in contentSelectors)
        {
            var node = doc.DocumentNode.SelectSingleNode(selector);
            if (node != null && !string.IsNullOrWhiteSpace(node.InnerText))
            {
                return node;
            }
        }

        return null;
    }

    private string ExtractText(HtmlNode node)
    {
        var sb = new StringBuilder();
        ExtractTextRecursive(node, sb);
        return sb.ToString();
    }

    private void ExtractTextRecursive(HtmlNode node, StringBuilder sb)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            var text = HtmlEntity.DeEntitize(node.InnerText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.Append(text.Trim());
                sb.Append(' ');
            }
            return;
        }

        if (node.Name == "br" || node.Name == "p" || node.Name == "div" || 
            node.Name == "h1" || node.Name == "h2" || node.Name == "h3" || 
            node.Name == "h4" || node.Name == "h5" || node.Name == "h6" ||
            node.Name == "li")
        {
            sb.AppendLine();
        }

        foreach (var child in node.ChildNodes)
        {
            ExtractTextRecursive(child, sb);
        }

        if (node.Name == "p" || node.Name == "div" || node.Name.StartsWith("h"))
        {
            sb.AppendLine();
        }
    }

    private string CleanText(string text)
    {
        text = Regex.Replace(text, @"\r\n|\r|\n", "\n");
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = Regex.Replace(text, @"^\s+", "", RegexOptions.Multiline);

        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        return string.Join("\n", lines).Trim();
    }
}
