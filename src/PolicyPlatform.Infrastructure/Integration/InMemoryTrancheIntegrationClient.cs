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

    /// <summary>Returns the seeded tranche for <paramref name="claimId"/>, if any. Never throws.</summary>
    /// <param name="claimId">Claim identifier to look up.</param>
    /// <param name="ct">Cancellation token (unused; this stand-in never awaits).</param>
    /// <returns>The seeded tranche, or <see langword="null"/> when none was seeded.</returns>
    public Task<LastPaidTrancheDto?> GetLastPaidTrancheAsync(Guid claimId, CancellationToken ct = default)
        => Task.FromResult(_tranches.GetValueOrDefault(claimId));

    /// <summary>Test/demo helper: registers the tranche that <see cref="GetLastPaidTrancheAsync"/> will return for a claim.</summary>
    /// <param name="claimId">Claim to seed.</param>
    /// <param name="tranche">Tranche to return for future lookups of <paramref name="claimId"/>.</param>
    public void Seed(Guid claimId, LastPaidTrancheDto tranche) => _tranches[claimId] = tranche;
}
