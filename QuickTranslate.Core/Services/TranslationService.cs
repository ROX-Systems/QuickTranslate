using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using Serilog;

namespace QuickTranslate.Core.Services;

public class TranslationService : ITranslationService
{
    private readonly IProviderClient _providerClient;
    private readonly ILogger _logger;

    public TranslationService(IProviderClient providerClient)
    {
        _providerClient = providerClient;
        _logger = Log.ForContext<TranslationService>();
    }

    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SourceText))
        {
            return TranslationResult.FromError("Source text is empty");
        }

        _logger.Information("Starting translation to {TargetLanguage}", request.TargetLanguage);

        var systemPrompt = BuildSystemPrompt(request);
        var userPrompt = request.SourceText;

        var result = await _providerClient.SendTranslationRequestAsync(systemPrompt, userPrompt, cancellationToken);

        if (result.Success)
        {
            result.TranslatedText = CleanTranslationOutput(result.TranslatedText);
        }

        return result;
    }

    private string BuildSystemPrompt(TranslationRequest request)
    {
        var targetLang = request.TargetLanguage;
        
        if (!string.IsNullOrEmpty(request.SourceLanguage))
        {
            return $@"You are a professional translator. Translate the following text from {request.SourceLanguage} to {targetLang}. 
Provide ONLY the translation without any explanations, notes, or additional text.
Preserve the original formatting, including line breaks and paragraphs.
If the text contains technical terms, translate them appropriately for the context."
                + GetProfileHint(request.Profile);
        }

        return $@"You are a professional translator. Detect the language of the following text and translate it to {targetLang}.
If the text is already in {targetLang}, translate it to English instead.
Provide ONLY the translation without any explanations, notes, or additional text.
Preserve the original formatting, including line breaks and paragraphs.
If the text contains technical terms, translate them appropriately for the context."
            + GetProfileHint(request.Profile);
    }

    private static string GetProfileHint(TranslationProfile? profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.SystemPromptHint))
            return string.Empty;
        
        return $"\n\nAdditional context: {profile.SystemPromptHint}";
    }

    private string CleanTranslationOutput(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = text.Trim();

        if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length > 2)
        {
            text = text[1..^1];
        }

        return text;
    }
}
