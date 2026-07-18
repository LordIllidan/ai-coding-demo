using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Infrastructure.Integration;

/// <summary>Local stand-in for the downstream tranche system. Swap for a real HTTP client
/// wired with a timeout policy and circuit breaker (raising TrancheServiceTimeoutException /
/// TrancheServiceUnavailableException on failure) once that integration exists.</summary>
public sealed class InMemoryTrancheIntegrationClient : ITrancheIntegrationClient
{
    private readonly ConcurrentDictionary<Guid, LastPaidTrancheDto> _tranches = new();

    public Task<LastPaidTrancheDto?> GetLastPaidTrancheAsync(Guid claimId, CancellationToken ct = default)
        => Task.FromResult(_tranches.GetValueOrDefault(claimId));

    public void Seed(Guid claimId, LastPaidTrancheDto tranche) => _tranches[claimId] = tranche;
}
