namespace QuickTranslate.Core.Services;

public static class LanguageNormalizer
{
    private static readonly string[] SupportedTtsLanguages = { "ru", "en", "de", "es", "fr", "it", "hi" };
    
    private static readonly string[] AllSupportedLanguages = 
    { 
        "ru", "en", "de", "es", "fr", "it", "hi", "zh", "ja", "ko", "pt", "ar", "tr", "pl", "uk"
    };

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "ru";

        var s = input.Trim().ToLowerInvariant();

        if (s.StartsWith("ru") || s.Contains("рус") || s.Contains("russian")) return "ru";
        if (s.StartsWith("en") || s.Contains("англ") || s.Contains("english")) return "en";
        if (s.StartsWith("de") || s.Contains("нем") || s.Contains("german") || s.Contains("deutsch")) return "de";
        if (s.StartsWith("fr") || s.Contains("фран") || s.Contains("french") || s.Contains("français")) return "fr";
        if (s.StartsWith("es") || s.Contains("испан") || s.Contains("spanish") || s.Contains("español")) return "es";
        if (s.StartsWith("it") || s.Contains("итал") || s.Contains("italian") || s.Contains("italiano")) return "it";
        if (s.StartsWith("hi") || s.Contains("хинди") || s.Contains("hindi")) return "hi";
        
        if (s.StartsWith("zh") || s.StartsWith("ch") || s.Contains("кит") || s.Contains("chinese") || s.Contains("中文")) return "zh";
        if (s.StartsWith("ja") || s.StartsWith("jp") || s.Contains("япон") || s.Contains("japanese") || s.Contains("日本語")) return "ja";
        if (s.StartsWith("ko") || s.StartsWith("kr") || s.Contains("корей") || s.Contains("korean") || s.Contains("한국어")) return "ko";
        
        if (s.StartsWith("pt") || s.Contains("португ") || s.Contains("portuguese") || s.Contains("português")) return "pt";
        if (s.StartsWith("ar") || s.Contains("араб") || s.Contains("arabic") || s.Contains("العربية")) return "ar";
        if (s.StartsWith("tr") || s.Contains("турец") || s.Contains("turkish") || s.Contains("türkçe")) return "tr";
        if (s.StartsWith("pl") || s.Contains("польск") || s.Contains("polish") || s.Contains("polski")) return "pl";
        if (s.StartsWith("uk") || s.Contains("укр") || s.Contains("ukrainian") || s.Contains("україн")) return "uk";

        return "ru";
    }

    public static bool IsTtsSupported(string languageCode)
    {
        var normalized = Normalize(languageCode);
        return SupportedTtsLanguages.Contains(normalized);
    }

    public static string[] GetSupportedTtsLanguages() => SupportedTtsLanguages;
    
    public static string[] GetAllSupportedLanguages() => AllSupportedLanguages;
    
    public static string GetLanguageDisplayName(string code) => code switch
    {
        "ru" => "Russian",
        "en" => "English",
        "de" => "German",
        "fr" => "French",
        "es" => "Spanish",
        "it" => "Italian",
        "hi" => "Hindi",
        "zh" => "Chinese",
        "ja" => "Japanese",
        "ko" => "Korean",
        "pt" => "Portuguese",
        "ar" => "Arabic",
        "tr" => "Turkish",
        "pl" => "Polish",
        "uk" => "Ukrainian",
        _ => code
    };
}
