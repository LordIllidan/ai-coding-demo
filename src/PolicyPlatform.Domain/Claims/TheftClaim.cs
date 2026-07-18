using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

public sealed class TheftClaim : Entity
{
    public Guid PolicyId { get; }
    public DateOnly IncidentDate { get; }
    public string Description { get; }
    public PoliceReportNumber PoliceReportNumber { get; }
    public DateTime ReportedAt { get; }

    /// <summary>Human-readable claim reference. There is no dedicated numbering scheme for
    /// claims yet (unlike PolicyNumber/SequentialPolicyNumberGenerator for policies), so this
    /// is derived from the id until that lands.</summary>
    public string ClaimNumber => $"SZK-{Id:N}"[..12].ToUpperInvariant();

    private TheftClaim(
        Guid id, Guid policyId, DateOnly incidentDate, string description,
        PoliceReportNumber policeReportNumber, DateTime reportedAt)
        : base(id)
    {
        PolicyId = policyId;
        IncidentDate = incidentDate;
        Description = description;
        PoliceReportNumber = policeReportNumber;
        ReportedAt = reportedAt;
    }

    public static TheftClaim Register(
        Guid id, Guid policyId, DateOnly incidentDate, string description,
        PoliceReportNumber policeReportNumber, DateTime reportedAt)
    {
        if (policyId == Guid.Empty)
        {
            throw new DomainException("Theft claim must reference a valid policy.");
        }

        return new TheftClaim(id, policyId, incidentDate, description, policeReportNumber, reportedAt);
    }
}
