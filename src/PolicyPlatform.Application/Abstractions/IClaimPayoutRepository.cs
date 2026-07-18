using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Abstractions;

public interface IClaimPayoutRepository
{
    /// <summary>Returns the most recently paid installment for the given customer
    /// (status = Paid, ordered by PaidAt descending), or null if none exists. The
    /// customerId must come from the caller's authenticated identity — never from
    /// client-supplied input.</summary>
    Task<ClaimPayout?> GetLastPaidForCustomerAsync(Guid customerId, CancellationToken ct = default);
}
