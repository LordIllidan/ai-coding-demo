using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

/// <summary>Request body for POST /api/theft-claims. <see cref="PoliceReportNumber"/> is
/// validated (required, 3-50 chars, letters/digits/space/"/"/"-") and normalized to
/// UPPERCASE by the <see cref="Domain.Claims.PoliceReportNumber"/> value object.</summary>
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

/// <summary>Response body for GET /api/theft-claims/{id}.</summary>
public sealed record TheftClaimDto(
    Guid Id,
    Guid PolicyId,
    string PoliceReportNumber,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>Maps a <see cref="TheftClaim"/> domain entity to its DTO.</summary>
    public static TheftClaimDto FromDomain(TheftClaim claim) => new(
        claim.Id,
        claim.PolicyId,
        claim.PoliceReportNumber.Value,
        claim.Status,
        claim.CreatedAt,
        claim.UpdatedAt);
}

/// <summary>Single field-level validation failure reported in a <see cref="ValidationErrorResponse"/>.</summary>
public sealed record FieldError(string Field, string Code, string Message);

/// <summary>422 response body for POST /api/theft-claims validation failures (AISDLC-51 contract).</summary>
public sealed record ValidationErrorResponse(string Code, IReadOnlyList<FieldError> FieldErrors);
