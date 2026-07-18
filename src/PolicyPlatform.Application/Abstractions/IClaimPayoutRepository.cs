using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Abstractions;

/// <summary>Read access to claim payout records for the mobile "last payout" use case.</summary>
public interface IClaimPayoutRepository
{
    /// <summary>Returns the most recently paid payout for the customer, or null if none exists.
    /// Callers must pass only a customer id derived from the authenticated request (JWT subject) —
    /// never one supplied by the client in a path/query/body parameter.</summary>
    /// <param name="customerId">Id of the authenticated customer.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The most recent <see cref="ClaimPayoutStatus.Paid"/> payout, or null if none exists.</returns>
    Task<ClaimPayout?> GetLastPaidPayoutAsync(Guid customerId, CancellationToken ct = default);
}
