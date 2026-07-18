using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Application.Claims;

/// <summary>Read-only use case backing the mobile "last payout" screen. Scoping to a
/// customer is a required parameter here — callers must derive it from the authenticated
/// JWT subject, never from client-supplied path/query/body input.</summary>
public sealed class ClaimPayoutService
{
    private readonly IClaimPayoutRepository _payouts;

    /// <param name="payouts">Repository providing paid-installment lookups.</param>
    public ClaimPayoutService(IClaimPayoutRepository payouts) => _payouts = payouts;

    /// <summary>Gets the customer's most recently paid claim installment, mapped to the mobile read contract.</summary>
    /// <param name="customerId">Customer identifier derived from the caller's JWT.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mapped <see cref="LastPayoutDto"/>, or <see langword="null"/> if the customer has no paid installment.</returns>
    public async Task<LastPayoutDto?> GetLastPayoutForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var payout = await _payouts.GetLastPaidForCustomerAsync(customerId, ct);
        return payout is null ? null : LastPayoutDto.FromDomain(payout);
    }
}
