namespace PolicyPlatform.Domain.Claims;

/// <summary>Lifecycle state of a claim installment. Only <see cref="Paid"/> installments
/// are eligible for the mobile "last payout" read.</summary>
public enum ClaimPayoutStatus
{
    /// <summary>Approved but not yet disbursed.</summary>
    Pending,

    /// <summary>Disbursed to the customer — the only status surfaced by the mobile read API.</summary>
    Paid,

    /// <summary>Declined; never disbursed.</summary>
    Rejected,
}
