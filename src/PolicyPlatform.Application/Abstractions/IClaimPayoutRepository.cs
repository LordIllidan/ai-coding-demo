using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Abstractions;

public interface IClaimPayoutRepository
{
    /// <summary>Returns the most recently paid payout for the customer, or null if none exists.
    /// Callers must pass only a customer id derived from the authenticated request (JWT subject) —
    /// never one supplied by the client in a path/query/body parameter.</summary>
    Task<ClaimPayout?> GetLastPaidPayoutAsync(Guid customerId, CancellationToken ct = default);
}
