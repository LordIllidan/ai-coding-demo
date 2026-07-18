using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Abstractions;

/// <summary>Read access to paid claim installments.</summary>
public interface IClaimPayoutRepository
{
    /// <summary>Returns the most recently paid installment for the given customer
    /// (status = Paid, ordered by PaidAt descending), or null if none exists. The
    /// customerId must come from the caller's authenticated identity — never from
    /// client-supplied input.</summary>
    /// <param name="customerId">Customer identifier derived from the caller's JWT.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The latest paid <see cref="ClaimPayout"/>, or <see langword="null"/> if the customer has none.</returns>
    Task<ClaimPayout?> GetLastPaidForCustomerAsync(Guid customerId, CancellationToken ct = default);
}
