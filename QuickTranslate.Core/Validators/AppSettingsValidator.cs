using FluentValidation;
using QuickTranslate.Core.Models;

namespace QuickTranslate.Core.Validators;

public class AppSettingsValidator : AbstractValidator<AppSettings>
{
    private static readonly string[] ValidThemes = { "OceanBlue", "Emerald", "Sunset", "Purple", "Monochrome" };
    private static readonly string[] ValidInterfaceLanguages = { "ru", "en", "os" };
    private static readonly string[] ValidProfiles = { "general", "technical", "literary", "legal", "medical", "casual" };

    public AppSettingsValidator()
    {
        RuleFor(x => x.Providers)
            .NotEmpty().WithMessage("At least one provider must be configured")
            .Must(HaveAtLeastOneValidProvider).WithMessage("At least one provider must have valid configuration");

        RuleForEach(x => x.Providers)
            .SetValidator(new ProviderConfigValidator());

        RuleFor(x => x.ActiveProviderId)
            .Must((settings, providerId) => string.IsNullOrEmpty(providerId) || settings.Providers.Any(p => p.Id == providerId))
            .WithMessage("Active provider must reference an existing provider");

        RuleFor(x => x.TargetLanguage)
            .NotEmpty().WithMessage("Target language is required")
            .MaximumLength(50).WithMessage("Target language must not exceed 50 characters");

        RuleFor(x => x.InterfaceLanguage)
            .Must(BeValidInterfaceLanguage).WithMessage("Interface language must be one of: ru, en, os");

        RuleFor(x => x.ColorTheme)
            .Must(BeValidTheme).WithMessage("Theme must be one of: OceanBlue, Emerald, Sunset, Purple, Monochrome");

        RuleFor(x => x.ActiveProfileId)
            .Must(BeValidProfile).WithMessage("Profile must be one of: general, technical, literary, legal, medical, casual");

        RuleFor(x => x.TtsEndpoint)
            .NotEmpty().WithMessage("TTS endpoint is required")
            .Must(BeValidUrl).WithMessage("TTS endpoint must be a valid URL");
    }

    private static bool HaveAtLeastOneValidProvider(List<ProviderConfig> providers)
    {
        var validator = new ProviderConfigValidator();
        return providers.Any(p => validator.Validate(p).IsValid);
    }

    private static bool BeValidInterfaceLanguage(string? language)
    {
        return language == null || ValidInterfaceLanguages.Contains(language);
    }

    private static bool BeValidTheme(string? theme)
    {
        return theme == null || ValidThemes.Contains(theme);
    }

    private static bool BeValidProfile(string profile)
    {
        return ValidProfiles.Contains(profile);
    }

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
