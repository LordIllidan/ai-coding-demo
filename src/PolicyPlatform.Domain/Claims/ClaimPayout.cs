using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Domain.Claims;

/// <summary>A single disbursement of a claim, as recorded in the claim_payout table. Only
/// payouts with <see cref="ClaimPayoutStatus.Paid"/> are ever exposed to the mobile
/// "last payout" screen.</summary>
public sealed class ClaimPayout : Entity
{
    /// <summary>Id of the claim this payout was disbursed for.</summary>
    public Guid ClaimId { get; }

    /// <summary>Human-readable claim number shown to the customer.</summary>
    public string ClaimNumber { get; }

    /// <summary>Id of the customer this payout belongs to. Always sourced from the
    /// authenticated request context, never from client input.</summary>
    public Guid CustomerId { get; }

    /// <summary>Gross payout amount and currency.</summary>
    public Money Amount { get; }

    /// <summary>Timestamp the payout was made.</summary>
    public DateTime PaidAt { get; }

    /// <summary>Lifecycle status of the payout.</summary>
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

    /// <summary>Creates a new claim payout after validating required references.</summary>
    /// <param name="id">Entity id.</param>
    /// <param name="claimId">Id of the claim being paid out; must not be empty.</param>
    /// <param name="claimNumber">Human-readable claim number; must not be blank.</param>
    /// <param name="customerId">Id of the owning customer; must not be empty.</param>
    /// <param name="amount">Gross payout amount and currency.</param>
    /// <param name="paidAt">Timestamp the payout was made.</param>
    /// <param name="status">Lifecycle status of the payout.</param>
    /// <returns>The newly created <see cref="ClaimPayout"/>.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="claimId"/> or
    /// <paramref name="customerId"/> is empty, or <paramref name="claimNumber"/> is blank.</exception>
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
