using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

/// <summary>A vehicle theft claim registered against a policy.</summary>
public sealed class TheftClaim : Entity
{
    /// <summary>Status assigned to every newly registered claim (AISDLC-51 contract).</summary>
    public const string StatusAccepted = "ACCEPTED";

    public Guid PolicyId { get; }
    public PoliceReportNumber PoliceReportNumber { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }

    private TheftClaim(
        Guid id, Guid policyId, PoliceReportNumber policeReportNumber, string status,
        DateTime createdAt, DateTime updatedAt)
        : base(id)
    {
        PolicyId = policyId;
        PoliceReportNumber = policeReportNumber;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Registers a new theft claim with <see cref="StatusAccepted"/> status.</summary>
    /// <param name="id">Id to assign to the new claim.</param>
    /// <param name="policyId">Policy the claim is filed against; must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="policeReportNumber">Already-validated police report number.</param>
    /// <param name="registeredAt">Timestamp used for both <see cref="CreatedAt"/> and <see cref="UpdatedAt"/>.</param>
    /// <exception cref="DomainException"><paramref name="policyId"/> is <see cref="Guid.Empty"/>.</exception>
    public static TheftClaim Register(
        Guid id, Guid policyId, PoliceReportNumber policeReportNumber, DateTime registeredAt)
    {
        if (policyId == Guid.Empty)
        {
            throw new DomainException("Theft claim must reference a valid policy.");
        }

        return new TheftClaim(id, policyId, policeReportNumber, StatusAccepted, registeredAt, registeredAt);
    }
}
