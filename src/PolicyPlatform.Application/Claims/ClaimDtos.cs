using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

public sealed record CreateTheftClaimRequest(
    Guid PolicyId,
    DateOnly IncidentDate,
    string Description,
    string? PoliceReportNumber);

public sealed record TheftClaimDto(
    Guid Id,
    Guid PolicyId,
    DateOnly IncidentDate,
    string Description,
    string PoliceReportNumber,
    DateTime ReportedAt)
{
    public static TheftClaimDto FromDomain(TheftClaim claim) => new(
        claim.Id,
        claim.PolicyId,
        claim.IncidentDate,
        claim.Description,
        claim.PoliceReportNumber.Value,
        claim.ReportedAt);
}
