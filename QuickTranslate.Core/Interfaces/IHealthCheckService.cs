using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuickTranslate.Core.Models;

namespace QuickTranslate.Core.Interfaces;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckProviderHealthAsync(ProviderConfig provider, CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CheckTtsHealthAsync(string? ttsEndpoint, CancellationToken cancellationToken = default);
    Task<Dictionary<string, HealthCheckResult>> CheckAllProvidersAsync(CancellationToken cancellationToken = default);
}
