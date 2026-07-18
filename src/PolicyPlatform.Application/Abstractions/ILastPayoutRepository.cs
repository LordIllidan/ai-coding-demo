using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Application.Abstractions;

/// <summary>Read-only access to the last PAID claim_payout row for a customer. Callers pass
/// only the customerId resolved from the caller's JWT — never a client-supplied identifier.
/// Implementations must throw DataSourceTimeoutException (rather than falling back to a cached
/// or stale row) when the underlying data source is unreachable or times out.</summary>
public interface ILastPayoutRepository
{
    /// <summary>Reads the last PAID claim_payout row for the given customer, if any.</summary>
    /// <param name="customerId">Customer id resolved from the caller's JWT — never a client-supplied identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The last paid payout record, or <c>null</c> when the customer has none.</returns>
    /// <exception cref="PolicyPlatform.Application.Claims.DataSourceTimeoutException">The underlying data source is unreachable or times out.</exception>
    Task<LastPayoutRecord?> GetLastPayoutAsync(Guid customerId, CancellationToken ct = default);
}
