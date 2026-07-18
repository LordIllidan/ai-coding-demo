using System.Globalization;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Mobile;

/// <summary>Payout amount as returned by the mobile last-payout contract:
/// <c>Value</c> is a 2-decimal-place decimal string, <c>Currency</c> a 3-letter ISO code.</summary>
public sealed record LastPayoutAmountDto(string Value, string Currency);

/// <summary>Response body for <c>GET /api/mobile/me/claims/last-payout</c>. Always
/// <c>ReadOnly = true</c> — this contract has no corresponding write endpoint.</summary>
public sealed record LastPayoutResponse(
    string ClaimNumber,
    LastPayoutAmountDto Amount,
    string PayoutDate,
    bool ReadOnly = true)
{
    /// <summary>Maps a <see cref="ClaimPayout"/> domain entity to the mobile response contract.</summary>
    /// <param name="payout">The domain payout to map.</param>
    /// <returns>The mapped <see cref="LastPayoutResponse"/>.</returns>
    public static LastPayoutResponse FromDomain(ClaimPayout payout) => new(
        payout.ClaimNumber,
        new LastPayoutAmountDto(
            payout.Amount.Amount.ToString("F2", CultureInfo.InvariantCulture),
            payout.Amount.Currency),
        DateOnly.FromDateTime(payout.PaidAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
