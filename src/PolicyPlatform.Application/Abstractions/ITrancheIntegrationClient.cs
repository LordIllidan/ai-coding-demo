using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Application.Abstractions;

/// <summary>Live gateway to the downstream tranche system. Implementations own the
/// timeout/circuit-breaker policy and must throw TrancheServiceUnavailableException or
/// TrancheServiceTimeoutException on failure rather than returning a cached result —
/// callers must never fall back to stale data when this call fails.</summary>
public interface ITrancheIntegrationClient
{
    Task<LastPaidTrancheDto?> GetLastPaidTrancheAsync(Guid claimId, CancellationToken ct = default);
}
