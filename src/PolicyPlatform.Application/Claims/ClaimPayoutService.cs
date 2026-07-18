using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Application.Claims;

/// <summary>Read-only use case backing the mobile "last payout" screen. Scoping to a
/// customer is a required parameter here — callers must derive it from the authenticated
/// JWT subject, never from client-supplied path/query/body input.</summary>
public sealed class ClaimPayoutService
{
    private readonly IClaimPayoutRepository _payouts;

    public ClaimPayoutService(IClaimPayoutRepository payouts) => _payouts = payouts;

    public async Task<LastPayoutDto?> GetLastPayoutForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var payout = await _payouts.GetLastPaidForCustomerAsync(customerId, ct);
        return payout is null ? null : LastPayoutDto.FromDomain(payout);
    }
}
