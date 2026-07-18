using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Application.Abstractions;

/// <summary>Live gateway to the downstream tranche system. Implementations own the
/// timeout/circuit-breaker policy and must throw TrancheServiceUnavailableException or
/// TrancheServiceTimeoutException on failure rather than returning a cached result —
/// callers must never fall back to stale data when this call fails.</summary>
public interface ITrancheIntegrationClient
{
    /// <summary>Fetches the last paid tranche for <paramref name="claimId"/> from the downstream tranche system.</summary>
    /// <param name="claimId">Claim identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The last paid tranche, or <see langword="null"/> when the claim has none.</returns>
    /// <exception cref="PolicyPlatform.Application.Claims.TrancheServiceUnavailableException">The downstream service is unreachable or its circuit breaker is open.</exception>
    /// <exception cref="PolicyPlatform.Application.Claims.TrancheServiceTimeoutException">The downstream call did not complete within the configured timeout.</exception>
    Task<LastPaidTrancheDto?> GetLastPaidTrancheAsync(Guid claimId, CancellationToken ct = default);
}
