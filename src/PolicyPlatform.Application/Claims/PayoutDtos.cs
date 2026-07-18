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

public sealed record LastPaidInstallmentDto(
    Guid InstallmentId,
    int InstallmentNo,
    DateOnly PaidAt,
    decimal Amount,
    string Currency);

public sealed record ClaimLastPaidInstallmentResponse(
    Guid ClaimId,
    string ScreenState,
    LastPaidInstallmentDto? LastPaidInstallment,
    bool CanEdit)
{
    // Omitted from the payload entirely (not just null) for NO_PAYOUT/INCOMPLETE_DATA, matching
    // the contract's examples where the key is absent rather than present-and-null.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClaimNumber { get; init; }

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
