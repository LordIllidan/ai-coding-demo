using System.Globalization;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

/// <summary>Wire-format money value: decimal amount fixed to 2 places, ISO 4217 currency code.</summary>
/// <param name="Value">Decimal amount formatted to 2 places (e.g. "1234.50").</param>
/// <param name="Currency">3-letter ISO currency code (e.g. "PLN").</param>
public sealed record MoneyDto(string Value, string Currency);

/// <summary>Response contract for GET /api/mobile/me/claims/last-payout.</summary>
/// <param name="ClaimNumber">Human-facing number of the claim the payout belongs to.</param>
/// <param name="Amount">Disbursed amount and currency.</param>
/// <param name="PayoutDate">Disbursement date, formatted "yyyy-MM-dd".</param>
/// <param name="ReadOnly">Always <see langword="true"/> — this screen accepts no writes.</param>
public sealed record LastPayoutDto(string ClaimNumber, MoneyDto Amount, string PayoutDate, bool ReadOnly)
{
    /// <summary>Maps a <see cref="ClaimPayout"/> domain entity to its wire contract.</summary>
    /// <param name="payout">The paid installment to map.</param>
    /// <returns>The corresponding <see cref="LastPayoutDto"/>.</returns>
    public static LastPayoutDto FromDomain(ClaimPayout payout) => new(
        payout.ClaimNumber,
        new MoneyDto(payout.AmountGross.ToString("F2", CultureInfo.InvariantCulture), payout.CurrencyCode),
        DateOnly.FromDateTime(payout.PaidAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ReadOnly: true);
}
