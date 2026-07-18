using System.Globalization;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

public sealed record MoneyDto(string Value, string Currency);

public sealed record LastPayoutDto(string ClaimNumber, MoneyDto Amount, string PayoutDate, bool ReadOnly)
{
    public static LastPayoutDto FromDomain(ClaimPayout payout) => new(
        payout.ClaimNumber,
        new MoneyDto(payout.AmountGross.ToString("F2", CultureInfo.InvariantCulture), payout.CurrencyCode),
        DateOnly.FromDateTime(payout.PaidAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ReadOnly: true);
}
