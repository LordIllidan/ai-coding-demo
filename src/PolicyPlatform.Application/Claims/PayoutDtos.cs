using System.Text.Json.Serialization;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

/// <summary>Screen states for GET /claims/{claimId}/payouts/last-paid-installment, per the
/// TechLeadAgent contract for AISDLC-136. Literal values (not a JsonStringEnumConverter enum)
/// because the wire format is SCREAMING_SNAKE_CASE while the rest of the API is camelCase.</summary>
public static class PayoutScreenStates
{
    public const string Paid = "PAID";
    public const string NoPayout = "NO_PAYOUT";
    public const string IncompleteData = "INCOMPLETE_DATA";
}

/// <summary>Wire shape of a single paid installment, only ever populated when
/// screenState is PAID.</summary>
public sealed record LastPaidInstallmentDto(
    Guid InstallmentId,
    int InstallmentNo,
    DateOnly PaidAt,
    decimal Amount,
    string Currency);

/// <summary>Response for GET /claims/{claimId}/payouts/last-paid-installment.</summary>
public sealed record ClaimLastPaidInstallmentResponse(
    Guid ClaimId,
    string ScreenState,
    LastPaidInstallmentDto? LastPaidInstallment,
    bool CanEdit)
{
    // Omitted from the payload entirely (not just null) for NO_PAYOUT/INCOMPLETE_DATA, matching
    // the contract's examples where the key is absent rather than present-and-null.
    /// <summary>Human-readable claim reference. Present only when screenState is PAID.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClaimNumber { get; init; }

    /// <summary>Maps a claim and its raw payout record to the response contract, deciding
    /// PAID vs NO_PAYOUT vs INCOMPLETE_DATA.</summary>
    /// <param name="claim">The claim the payout belongs to.</param>
    /// <param name="record">The raw last-paid-installment row, or null when the claim has
    /// no paid installments.</param>
    /// <returns>A response with screenState NO_PAYOUT when <paramref name="record"/> is null,
    /// INCOMPLETE_DATA when any of its fields are missing/invalid, or PAID with the mapped
    /// installment otherwise.</returns>
    public static ClaimLastPaidInstallmentResponse From(TheftClaim claim, PayoutRecord? record)
    {
        if (record is null)
        {
            return new ClaimLastPaidInstallmentResponse(claim.Id, PayoutScreenStates.NoPayout, null, CanEdit: false);
        }

        if (record.InstallmentId is not { } installmentId
            || record.InstallmentNo is not { } installmentNo || installmentNo < 1
            || record.PaidAt is not { } paidAt
            || record.Amount is not { } amount
            || string.IsNullOrWhiteSpace(record.Currency))
        {
            return new ClaimLastPaidInstallmentResponse(claim.Id, PayoutScreenStates.IncompleteData, null, CanEdit: false);
        }

        var installment = new LastPaidInstallmentDto(
            installmentId, installmentNo, paidAt, amount, record.Currency.Trim().ToUpperInvariant());

        return new ClaimLastPaidInstallmentResponse(
            claim.Id, PayoutScreenStates.Paid, installment, CanEdit: false)
        {
            ClaimNumber = claim.ClaimNumber,
        };
    }
}
