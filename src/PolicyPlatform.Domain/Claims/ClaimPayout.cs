using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Domain.Claims;

public sealed class ClaimPayout : Entity
{
    public Guid ClaimId { get; }
    public string ClaimNumber { get; }
    public Guid CustomerId { get; }
    public Money Amount { get; }
    public DateTime PaidAt { get; }
    public ClaimPayoutStatus Status { get; }

    private ClaimPayout(
        Guid id, Guid claimId, string claimNumber, Guid customerId,
        Money amount, DateTime paidAt, ClaimPayoutStatus status)
        : base(id)
    {
        ClaimId = claimId;
        ClaimNumber = claimNumber;
        CustomerId = customerId;
        Amount = amount;
        PaidAt = paidAt;
        Status = status;
    }

    public static ClaimPayout Create(
        Guid id, Guid claimId, string claimNumber, Guid customerId,
        Money amount, DateTime paidAt, ClaimPayoutStatus status)
    {
        if (claimId == Guid.Empty)
        {
            throw new DomainException("Claim payout must reference a valid claim.");
        }

        if (string.IsNullOrWhiteSpace(claimNumber))
        {
            throw new DomainException("Claim payout must reference a claim number.");
        }

        if (customerId == Guid.Empty)
        {
            throw new DomainException("Claim payout must reference a valid customer.");
        }

        return new ClaimPayout(id, claimId, claimNumber, customerId, amount, paidAt, status);
    }
}
