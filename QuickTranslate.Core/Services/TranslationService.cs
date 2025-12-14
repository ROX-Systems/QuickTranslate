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
        var basePrompt = BuildBasePrompt(request, targetLang);
        
        if (request.UseAutoDetection)
        {
            return basePrompt + GetAutoDetectionHint();
        }
        
        return basePrompt + GetProfileHint(request.Profile);
    }

    private static string BuildBasePrompt(TranslationRequest request, string targetLang)
    {
        if (!string.IsNullOrEmpty(request.SourceLanguage))
        {
            return $@"You are a professional translator. Translate the following text from {request.SourceLanguage} to {targetLang}. 
Provide ONLY the translation without any explanations, notes, or additional text.
Preserve the original formatting, including line breaks and paragraphs.";
        }

        return $@"You are a professional translator. Detect the language of the following text and translate it to {targetLang}.
If the text is already in {targetLang}, translate it to English instead.
Provide ONLY the translation without any explanations, notes, or additional text.
Preserve the original formatting, including line breaks and paragraphs.";
    }

    private static string GetAutoDetectionHint()
    {
        return @"

First, analyze the text to determine its type/domain (technical documentation, literary fiction, legal document, medical text, casual conversation, or general text).
Then apply the appropriate translation style:
- For technical documentation: preserve code snippets, API names, variable names, and technical terms without translation.
- For literary/fiction: preserve the author's style and adapt idioms naturally while maintaining emotional impact.
- For legal documents: use formal legal terminology and maintain precise wording.
- For medical text: use proper medical terminology and keep Latin terms where conventional.
- For casual text: use conversational tone, slang is acceptable.
- For general text: use balanced, neutral translation style.

Apply the detected style automatically without mentioning it in the output.";
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
