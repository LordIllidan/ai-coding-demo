using System.Globalization;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Mobile;

public sealed record LastPayoutAmountDto(string Value, string Currency);

public sealed record LastPayoutResponse(
    string ClaimNumber,
    LastPayoutAmountDto Amount,
    string PayoutDate,
    bool ReadOnly = true)
{
    public static LastPayoutResponse FromDomain(ClaimPayout payout) => new(
        payout.ClaimNumber,
        new LastPayoutAmountDto(
            payout.Amount.Amount.ToString("F2", CultureInfo.InvariantCulture),
            payout.Amount.Currency),
        DateOnly.FromDateTime(payout.PaidAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
