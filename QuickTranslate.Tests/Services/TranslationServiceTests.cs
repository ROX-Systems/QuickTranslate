using FluentAssertions;
using Moq;
using QuickTranslate.Core.Interfaces;
using QuickTranslate.Core.Models;
using QuickTranslate.Core.Services;
using Xunit;

namespace QuickTranslate.Tests.Services;

public class TranslationServiceTests
{
    private readonly Mock<IProviderClient> _mockProviderClient;
    private readonly TranslationService _translationService;

    public TranslationServiceTests()
    {
        _mockProviderClient = new Mock<IProviderClient>();
        _translationService = new TranslationService(_mockProviderClient.Object);
    }

    [Fact]
    public async Task TranslateAsync_WhenSourceTextIsEmpty_ReturnsError()
    {
        var request = new TranslationRequest
        {
            SourceText = "",
            TargetLanguage = "English"
        };

        var result = await _translationService.TranslateAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Source text is empty");
        result.TranslatedText.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task TranslateAsync_WhenTranslationSucceeds_ReturnsSuccess()
    {
        var request = new TranslationRequest
        {
            SourceText = "Hello world",
            TargetLanguage = "Russian",
            SourceLanguage = "English"
        };

        _mockProviderClient
            .Setup(x => x.SendTranslationRequestAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TranslationResult.FromSuccess("Привет мир"));

        var result = await _translationService.TranslateAsync(request);

        result.Success.Should().BeTrue();
        result.TranslatedText.Should().Be("Привет мир");
        result.ErrorMessage.Should().BeNullOrEmpty();
        _mockProviderClient.Verify(
            x => x.SendTranslationRequestAsync(
                It.Is<string>(s => s.Contains("Translate")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_WhenProviderReturnsError_ReturnsError()
    {
        var request = new TranslationRequest
        {
            SourceText = "Hello",
            TargetLanguage = "Russian"
        };

        _mockProviderClient
            .Setup(x => x.SendTranslationRequestAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TranslationResult.FromError("API Error: 401 Unauthorized"));

        var result = await _translationService.TranslateAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("401 Unauthorized");
        result.TranslatedText.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task TranslateAsync_WhenProfileIsGeneral_UsesGeneralPrompt()
    {
        var request = new TranslationRequest
        {
            SourceText = "Hello",
            TargetLanguage = "Russian",
            Profile = new TranslationProfile
            {
                Id = "general",
                NameKey = "Profile_General",
                SystemPromptHint = ""
            }
        };

        _mockProviderClient
            .Setup(x => x.SendTranslationRequestAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TranslationResult.FromSuccess("Привет"));

        await _translationService.TranslateAsync(request);

        _mockProviderClient.Verify(
            x => x.SendTranslationRequestAsync(
                It.Is<string>(s => !s.Contains("Technical") && !s.Contains("literary")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_WhenProfileIsTechnical_UsesTechnicalPrompt()
    {
        var request = new TranslationRequest
        {
            SourceText = "const x = 5;",
            TargetLanguage = "Russian",
            Profile = new TranslationProfile
            {
                Id = "technical",
                NameKey = "Profile_Technical",
                SystemPromptHint = "This is technical documentation. Preserve code snippets, API names, variable names, and technical terms without translation. Use precise technical terminology."
            }
        };

        _mockProviderClient
            .Setup(x => x.SendTranslationRequestAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TranslationResult.FromSuccess("Перевод"));

        await _translationService.TranslateAsync(request);

        _mockProviderClient.Verify(
            x => x.SendTranslationRequestAsync(
                It.Is<string>(s => s.Contains("technical documentation") && s.Contains("code")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_WhenSourceLanguageSpecified_IncludesInPrompt()
    {
        var request = new TranslationRequest
        {
            SourceText = "Hello",
            TargetLanguage = "Russian",
            SourceLanguage = "English"
        };

        _mockProviderClient
            .Setup(x => x.SendTranslationRequestAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TranslationResult.FromSuccess("Привет"));

        await _translationService.TranslateAsync(request);

        _mockProviderClient.Verify(
            x => x.SendTranslationRequestAsync(
                It.Is<string>(s => s.Contains("English")),
                It.Is<string>(s => s.Contains("Hello")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_WhenResponseContainsMarkdown_PreservesMarkdown()
    {
        var request = new TranslationRequest
        {
            SourceText = "# Header\nContent",
            TargetLanguage = "Russian"
        };

        var markdownResponse = "# Заголовок\nСодержимое";

        _mockProviderClient
            .Setup(x => x.SendTranslationRequestAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TranslationResult.FromSuccess(markdownResponse));

        var result = await _translationService.TranslateAsync(request);

        result.Success.Should().BeTrue();
        result.TranslatedText.Should().Be(markdownResponse);
    }

    [Fact]
    public async Task TranslateAsync_WhenSourceTextIsLong_TranslatesSuccessfully()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 1000));
        var request = new TranslationRequest
        {
            SourceText = longText,
            TargetLanguage = "Russian"
        };

        _mockProviderClient
            .Setup(x => x.SendTranslationRequestAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TranslationResult.FromSuccess("Перевод длинного текста"));

        var result = await _translationService.TranslateAsync(request);

        result.Success.Should().BeTrue();
        result.TranslatedText.Should().Be("Перевод длинного текста");
    }
}
