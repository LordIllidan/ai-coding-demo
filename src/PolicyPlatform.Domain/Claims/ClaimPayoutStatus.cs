namespace PolicyPlatform.Domain.Claims;

/// <summary>Lifecycle status of a <see cref="ClaimPayout"/>.</summary>
public enum ClaimPayoutStatus
{
    /// <summary>Payout has been approved but not yet disbursed.</summary>
    Pending,

    /// <summary>Payout has been disbursed. Only payouts in this status are eligible
    /// for the mobile "last payout" screen.</summary>
    Paid,

    /// <summary>Payout was rejected and will not be disbursed.</summary>
    Rejected
}
