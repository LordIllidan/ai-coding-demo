using System.Data.Common;
using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Application.Mobile;

/// <summary>Application service (use-case layer) for the mobile "last payout" read-only screen.
/// The customer id must come from the authenticated request (JWT subject) — this service never
/// accepts one supplied by the client.</summary>
public sealed class MobileClaimPayoutService
{
    private readonly IClaimPayoutRepository _payouts;

    public MobileClaimPayoutService(IClaimPayoutRepository payouts) => _payouts = payouts;

    public async Task<LastPayoutResponse> GetLastPayoutAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var payout = await _payouts.GetLastPaidPayoutAsync(customerId, ct);
            return payout is null
                ? throw new LastPayoutNotFoundException()
                : LastPayoutResponse.FromDomain(payout);
        }
        catch (Exception ex) when (ex is TimeoutException or DbException)
        {
            throw new DataSourceUnavailableException(ex);
        }
    }
}
