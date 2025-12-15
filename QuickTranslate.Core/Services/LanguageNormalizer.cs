namespace QuickTranslate.Core.Services;

public static class LanguageNormalizer
{
    private static readonly string[] SupportedTtsLanguages = { "ru", "en", "de", "es", "fr", "it", "hi" };

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "ru";

        var s = input.Trim().ToLowerInvariant();

        if (s.StartsWith("ru") || s.Contains("рус") || s.Contains("russian")) return "ru";
        if (s.StartsWith("en") || s.Contains("англ") || s.Contains("english")) return "en";
        if (s.StartsWith("de") || s.Contains("нем") || s.Contains("german") || s.Contains("deutsch")) return "de";
        if (s.StartsWith("fr") || s.Contains("фран") || s.Contains("french")) return "fr";
        if (s.StartsWith("es") || s.Contains("испан") || s.Contains("spanish")) return "es";
        if (s.StartsWith("it") || s.Contains("итал") || s.Contains("italian")) return "it";
        if (s.StartsWith("hi") || s.Contains("хинди") || s.Contains("hindi")) return "hi";

        return "ru";
    }

    public static bool IsTtsSupported(string languageCode)
    {
        var normalized = Normalize(languageCode);
        return SupportedTtsLanguages.Contains(normalized);
    }

    public static string[] GetSupportedTtsLanguages() => SupportedTtsLanguages;
}
