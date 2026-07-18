using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

/// <summary>A paid installment of a claim (claim_payout row), carrying the parent claim's
/// number denormalized for read-only display — mobile "last payout" is a pure read use case,
/// there is no write path back into this aggregate.</summary>
public sealed class ClaimPayout : Entity
{
    public Guid ClaimId { get; }
    public string ClaimNumber { get; }
    public Guid CustomerId { get; }
    public decimal AmountGross { get; }
    public string CurrencyCode { get; }
    public DateTime PaidAt { get; }
    public ClaimPayoutStatus Status { get; }

    private ClaimPayout(
        Guid id, Guid claimId, string claimNumber, Guid customerId,
        decimal amountGross, string currencyCode, DateTime paidAt, ClaimPayoutStatus status)
        : base(id)
    {
        ClaimId = claimId;
        ClaimNumber = claimNumber;
        CustomerId = customerId;
        AmountGross = amountGross;
        CurrencyCode = currencyCode;
        PaidAt = paidAt;
        Status = status;
    }

    public static ClaimPayout Register(
        Guid id, Guid claimId, string claimNumber, Guid customerId,
        decimal amountGross, string currencyCode, DateTime paidAt, ClaimPayoutStatus status)
    {
        if (claimId == Guid.Empty)
        {
            throw new DomainException("Claim payout must reference a valid claim.");
        }

        if (customerId == Guid.Empty)
        {
            throw new DomainException("Claim payout must reference a valid customer.");
        }

        if (string.IsNullOrWhiteSpace(claimNumber))
        {
            throw new DomainException("Claim payout must carry the parent claim's number.");
        }

        if (amountGross < 0)
        {
            throw new DomainException("Claim payout amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
        {
            throw new DomainException($"'{currencyCode}' is not a valid ISO currency code.");
        }

        return new ClaimPayout(
            id, claimId, claimNumber.Trim(), customerId, amountGross, currencyCode.Trim().ToUpperInvariant(),
            paidAt, status);
    }
}
