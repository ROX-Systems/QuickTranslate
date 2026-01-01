using FluentValidation;
using QuickTranslate.Core.Models;

namespace QuickTranslate.Core.Validators;

public class ProviderConfigValidator : AbstractValidator<ProviderConfig>
{
    public ProviderConfigValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Provider name is required")
            .MaximumLength(100).WithMessage("Provider name must not exceed 100 characters");

        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("Base URL is required")
            .Must(BeValidUrl).WithMessage("Base URL must be a valid URL")
            .Must(EndsWithVersionPath).WithMessage("Base URL should end with version path (e.g., /v1)");

        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("API key is required")
            .MinimumLength(10).WithMessage("API key must be at least 10 characters long");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model name is required")
            .MaximumLength(100).WithMessage("Model name must not exceed 100 characters");

        RuleFor(x => x.Temperature)
            .InclusiveBetween(0, 2).WithMessage("Temperature must be between 0 and 2");

        RuleFor(x => x.MaxTokens)
            .GreaterThan(0).WithMessage("Max tokens must be greater than 0")
            .LessThanOrEqualTo(32000).WithMessage("Max tokens must not exceed 32000");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0).WithMessage("Timeout must be greater than 0 seconds")
            .LessThanOrEqualTo(300).WithMessage("Timeout must not exceed 300 seconds");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private static bool EndsWithVersionPath(string url)
    {
        return url.TrimEnd('/').EndsWith("/v1", StringComparison.OrdinalIgnoreCase);
    }
}
