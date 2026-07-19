using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

/// <summary>Lifecycle state of a <see cref="TheftClaim"/>. Only <see cref="Accepted"/> exists
/// today (AISDLC-51 contract) — a claim that passes validation is accepted outright.</summary>
public enum TheftClaimStatus
{
    Accepted,
}

/// <summary>A vehicle theft claim registered against a policy. Minimal aggregate per the
/// AISDLC-51 contract: policy reference, validated police report number, status, and
/// timestamps.</summary>
public sealed class TheftClaim : Entity
{
    /// <summary>Policy the claim is filed against.</summary>
    public Guid PolicyId { get; }

    /// <summary>Validated, normalized (UPPERCASE) police report number.</summary>
    public PoliceReportNumber PoliceReportNumber { get; }

    /// <summary>Current lifecycle status of the claim.</summary>
    public TheftClaimStatus Status { get; }

    /// <summary>UTC timestamp the claim was created.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>UTC timestamp the claim was last updated.</summary>
    public DateTime UpdatedAt { get; }

    private TheftClaim(
        Guid id, Guid policyId, PoliceReportNumber policeReportNumber, TheftClaimStatus status,
        DateTime createdAt, DateTime updatedAt)
        : base(id)
    {
        PolicyId = policyId;
        PoliceReportNumber = policeReportNumber;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Registers a new theft claim, already accepted (no separate review step).</summary>
    /// <param name="id">New claim identifier.</param>
    /// <param name="policyId">Policy the claim is filed against; must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="policeReportNumber">Already-validated police report number (see <see cref="PoliceReportNumber.TryCreate"/>).</param>
    /// <param name="now">Current UTC time, used for <see cref="CreatedAt"/> and <see cref="UpdatedAt"/>.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="policyId"/> is <see cref="Guid.Empty"/>.</exception>
    public static TheftClaim Register(Guid id, Guid policyId, PoliceReportNumber policeReportNumber, DateTime now)
    {
        if (policyId == Guid.Empty)
        {
            throw new DomainException("Theft claim must reference a valid policy.");
        }

        return new TheftClaim(id, policyId, policeReportNumber, TheftClaimStatus.Accepted, now, now);
    }
}
