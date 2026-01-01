using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Microsoft.Extensions.Http;
using QuickTranslate.Core.Models;
using CoreHealthCheckService = QuickTranslate.Core.Services.HealthCheckService;
using Xunit;

namespace QuickTranslate.Tests.Services;

public class HealthCheckServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly CoreHealthCheckService _healthCheckService;

    public HealthCheckServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _healthCheckService = new CoreHealthCheckService(_mockHttpClientFactory.Object);
    }

    [Fact]
    public async Task CheckProviderHealthAsync_WhenProviderHasNoApiKey_ReturnsUnhealthy()
    {
        var provider = new ProviderConfig
        {
            Id = "provider1",
            Name = "Test Provider",
            BaseUrl = "https://api.example.com/v1",
            ApiKey = ""
        };

        var result = await _healthCheckService.CheckProviderHealthAsync(provider);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("API key is not configured");
    }

    [Fact]
    public async Task CheckAllProvidersAsync_ReturnsDictionary()
    {
        var result = await _healthCheckService.CheckAllProvidersAsync();

        result.Should().NotBeNull();
        result.Should().ContainKey("TTS");
    }

    [Fact]
    public async Task CheckTtsHealthAsync_WhenCalled_ReturnsResult()
    {
        var result = await _healthCheckService.CheckTtsHealthAsync("https://example.com");

        // Result may be unhealthy due to network, but should return a result
        result.Should().NotBeNull();
    }
}
