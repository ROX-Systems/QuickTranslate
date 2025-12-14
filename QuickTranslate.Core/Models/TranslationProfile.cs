namespace QuickTranslate.Core.Models;

public class TranslationProfile
{
    public string Id { get; set; } = string.Empty;
    public string NameKey { get; set; } = string.Empty;
    public string SystemPromptHint { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }

    public static List<TranslationProfile> GetBuiltInProfiles() =>
    [
        new()
        {
            Id = "general",
            NameKey = "Profile_General",
            SystemPromptHint = "",
            IsBuiltIn = true
        },
        new()
        {
            Id = "technical",
            NameKey = "Profile_Technical",
            SystemPromptHint = "This is technical documentation. Preserve code snippets, API names, variable names, and technical terms without translation. Use precise technical terminology.",
            IsBuiltIn = true
        },
        new()
        {
            Id = "literary",
            NameKey = "Profile_Literary",
            SystemPromptHint = "This is literary/fiction text. Preserve the author's style, tone, and voice. Adapt idioms and metaphors naturally to the target language while maintaining emotional impact.",
            IsBuiltIn = true
        },
        new()
        {
            Id = "legal",
            NameKey = "Profile_Legal",
            SystemPromptHint = "This is a legal document. Use formal legal terminology. Maintain precise wording and structure. Preserve legal terms in their standard translated form.",
            IsBuiltIn = true
        },
        new()
        {
            Id = "medical",
            NameKey = "Profile_Medical",
            SystemPromptHint = "This is medical/scientific text. Use proper medical terminology. Keep Latin terms where conventionally used. Accuracy is critical.",
            IsBuiltIn = true
        },
        new()
        {
            Id = "casual",
            NameKey = "Profile_Casual",
            SystemPromptHint = "This is casual/informal text. Use conversational tone. Slang and colloquialisms are acceptable. Make it sound natural.",
            IsBuiltIn = true
        }
    ];
}
