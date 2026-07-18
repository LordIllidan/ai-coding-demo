using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Application.Claims;

/// <summary>Application service backing GET /claims/{claimId}/payouts/last-paid-installment.</summary>
public sealed class PayoutService
{
    private readonly IClaimRepository _claims;
    private readonly IPayoutRepository _payouts;

    public PayoutService(IClaimRepository claims, IPayoutRepository payouts)
    {
        _claims = claims;
        _payouts = payouts;
    }

    /// <summary>Null means the claim itself does not exist (controller maps this to 404
    /// CLAIM_NOT_FOUND). A claim with no paid installments still returns a response, with
    /// screenState NO_PAYOUT.</summary>
    public async Task<ClaimLastPaidInstallmentResponse?> GetLastPaidInstallmentAsync(
        Guid claimId, CancellationToken ct = default)
    {
        var claim = await _claims.GetByIdAsync(claimId, ct);
        if (claim is null)
        {
            return null;
        }

        var record = await _payouts.GetLastPaidInstallmentAsync(claimId, ct);
        return ClaimLastPaidInstallmentResponse.From(claim, record);
    }
}
