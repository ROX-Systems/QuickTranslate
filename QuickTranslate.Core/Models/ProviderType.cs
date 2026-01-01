namespace QuickTranslate.Core.Models;

/// <summary>
/// Supported provider types
/// </summary>
public enum ProviderType
{
    /// <summary>
    /// OpenAI API and compatible providers (z.ai, Groq, Together, etc.)
    /// </summary>
    OpenAI,

    /// <summary>
    /// Anthropic Claude API
    /// </summary>
    Anthropic,

    /// <summary>
    /// Google Gemini API
    /// </summary>
    Google,

    /// <summary>
    /// Ollama (local inference)
    /// </summary>
    Ollama,

    /// <summary>
    /// Custom provider implementation
    /// </summary>
    Custom
}
