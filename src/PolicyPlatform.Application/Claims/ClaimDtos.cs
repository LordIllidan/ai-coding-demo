using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

/// <summary>Request body for <c>POST /api/theft-claims</c>.</summary>
/// <param name="PolicyId">UUID of the policy the claim is filed against.</param>
/// <param name="IncidentDate">Date the theft is reported to have occurred.</param>
/// <param name="Description">Free-text description of the incident.</param>
/// <param name="PoliceReportNumber">Raw police report number; validated and normalized to
/// UPPERCASE by <see cref="PoliceReportNumber"/> before the claim is persisted.</param>
public sealed record CreateTheftClaimRequest(
    Guid PolicyId,
    DateOnly IncidentDate,
    string Description,
    string? PoliceReportNumber);

/// <summary>API response shape for a registered theft claim, per the
/// <c>POST /api/theft-claims</c> contract.</summary>
/// <param name="ClaimId">UUID of the created claim.</param>
/// <param name="PolicyId">UUID of the policy the claim is filed against.</param>
/// <param name="PoliceReportNumber">Normalized (trimmed, upper-cased) police report number.</param>
/// <param name="Status">Processing status of the claim, currently always <c>ACCEPTED</c>.</param>
/// <param name="NextStepAllowed">Whether the claim process may advance to its next step.</param>
public sealed record TheftClaimDto(
    Guid ClaimId,
    Guid PolicyId,
    string PoliceReportNumber,
    string Status,
    bool NextStepAllowed)
{
    /// <summary>Projects a <see cref="TheftClaim"/> domain entity into its API DTO.</summary>
    /// <param name="claim">Domain entity to project.</param>
    /// <returns>The equivalent <see cref="TheftClaimDto"/>.</returns>
    public static TheftClaimDto FromDomain(TheftClaim claim) => new(
        claim.Id,
        claim.PolicyId,
        claim.PoliceReportNumber.Value,
        claim.Status,
        NextStepAllowed: claim.Status == "ACCEPTED");
}
