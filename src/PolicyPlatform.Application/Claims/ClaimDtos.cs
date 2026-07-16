using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

public sealed record CreateTheftClaimRequest(
    Guid PolicyId,
    DateOnly IncidentDate,
    string Description,
    string? PoliceReportNumber);

public sealed record TheftClaimDto(
    Guid ClaimId,
    Guid PolicyId,
    string PoliceReportNumber,
    string Status,
    bool NextStepAllowed)
{
    public static TheftClaimDto FromDomain(TheftClaim claim) => new(
        claim.Id,
        claim.PolicyId,
        claim.PoliceReportNumber.Value,
        claim.Status,
        NextStepAllowed: claim.Status == "ACCEPTED");
}
