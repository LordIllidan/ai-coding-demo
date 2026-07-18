using System.Globalization;

namespace PolicyPlatform.Application.Claims;

/// <summary>Raw projection of the last PAID claim_payout row joined with its claim, as read
/// from the data source. All values are already in the shape the mobile contract expects.</summary>
public sealed record LastPayoutRecord(
    string ClaimNumber,
    decimal AmountGross,
    string CurrencyCode,
    DateOnly PayoutDate);

public sealed record MoneyDto(string Value, string Currency);

/// <summary>Response body for GET /api/mobile/me/claims/last-payout. ReadOnly is always true —
/// this screen never accepts writes.</summary>
public sealed record LastPayoutResponse(string ClaimNumber, MoneyDto Amount, string PayoutDate, bool ReadOnly)
{
    public static LastPayoutResponse FromRecord(LastPayoutRecord record) => new(
        record.ClaimNumber,
        new MoneyDto(record.AmountGross.ToString("F2", CultureInfo.InvariantCulture), record.CurrencyCode),
        record.PayoutDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ReadOnly: true);
}

public sealed record ErrorEnvelope(string Code, string Message, bool Retryable, string CorrelationId);
