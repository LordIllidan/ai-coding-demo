using System.Globalization;

namespace PolicyPlatform.Application.Claims;

/// <summary>Raw projection of the last PAID claim_payout row joined with its claim, as read
/// from the data source. All values are already in the shape the mobile contract expects.</summary>
public sealed record LastPayoutRecord(
    string ClaimNumber,
    decimal AmountGross,
    string CurrencyCode,
    DateOnly PayoutDate);

/// <summary>Monetary amount for the mobile last-payout contract. <paramref name="Value"/> is a
/// decimal string with 2 places; <paramref name="Currency"/> is the 3-letter ISO code
/// (defaults to PLN at the source).</summary>
/// <param name="Value">Decimal amount formatted to 2 places, e.g. "1234.56".</param>
/// <param name="Currency">3-letter ISO currency code.</param>
public sealed record MoneyDto(string Value, string Currency);

/// <summary>Response body for GET /api/mobile/me/claims/last-payout. ReadOnly is always true —
/// this screen never accepts writes.</summary>
/// <param name="ClaimNumber">Human-readable claim number the payout belongs to.</param>
/// <param name="Amount">Gross payout amount and currency.</param>
/// <param name="PayoutDate">Payout date formatted as yyyy-MM-dd.</param>
/// <param name="ReadOnly">Always true; signals to the mobile client that no writes are accepted.</param>
public sealed record LastPayoutResponse(string ClaimNumber, MoneyDto Amount, string PayoutDate, bool ReadOnly)
{
    /// <summary>Projects a raw <see cref="LastPayoutRecord"/> into the mobile contract shape.</summary>
    /// <param name="record">Record read from the last-payout repository.</param>
    /// <returns>A response DTO with <c>ReadOnly</c> set to true.</returns>
    public static LastPayoutResponse FromRecord(LastPayoutRecord record) => new(
        record.ClaimNumber,
        new MoneyDto(record.AmountGross.ToString("F2", CultureInfo.InvariantCulture), record.CurrencyCode),
        record.PayoutDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ReadOnly: true);
}

/// <summary>Shared error envelope contract used for all last-payout error responses.</summary>
/// <param name="Code">Stable machine-readable error code, e.g. "AUTH_REQUIRED".</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Retryable">Whether retrying the request may succeed (true for transient failures).</param>
/// <param name="CorrelationId">Request trace identifier for support/log correlation.</param>
public sealed record ErrorEnvelope(string Code, string Message, bool Retryable, string CorrelationId);
