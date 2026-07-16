using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

public sealed class TheftClaim : Entity
{
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
