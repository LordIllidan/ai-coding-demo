using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Application.Abstractions;

/// <summary>Read-only access to the last PAID claim_payout row for a customer. Callers pass
/// only the customerId resolved from the caller's JWT — never a client-supplied identifier.
/// Implementations must throw DataSourceTimeoutException (rather than falling back to a cached
/// or stale row) when the underlying data source is unreachable or times out.</summary>
public interface ILastPayoutRepository
{
    Task<LastPayoutRecord?> GetLastPayoutAsync(Guid customerId, CancellationToken ct = default);
}
