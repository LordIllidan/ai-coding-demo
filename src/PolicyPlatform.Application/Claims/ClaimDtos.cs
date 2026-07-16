using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

public sealed record CreateTheftClaimRequest(Guid PolicyId, string? PoliceReportNumber);

/// <summary>201 response for POST /api/theft-claims (AISDLC-51 contract).</summary>
public sealed record TheftClaimCreatedResponse(
    Guid ClaimId,
    Guid PolicyId,
    string PoliceReportNumber,
    string Status,
    bool NextStepAllowed)
{
    public static TheftClaimCreatedResponse FromDomain(TheftClaim claim) => new(
        claim.Id,
        claim.PolicyId,
        claim.PoliceReportNumber.Value,
        claim.Status,
        NextStepAllowed: true);
}

public sealed record TheftClaimDto(
    Guid Id,
    Guid PolicyId,
    string PoliceReportNumber,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static TheftClaimDto FromDomain(TheftClaim claim) => new(
        claim.Id,
        claim.PolicyId,
        claim.PoliceReportNumber.Value,
        claim.Status,
        claim.CreatedAt,
        claim.UpdatedAt);
}

/// <summary>422 response body for POST /api/theft-claims validation failures (AISDLC-51 contract).</summary>
public sealed record FieldError(string Field, string Code, string Message);

public sealed record ValidationErrorResponse(string Code, IReadOnlyList<FieldError> FieldErrors);
