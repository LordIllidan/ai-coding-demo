using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

public sealed record CreateTheftClaimRequest(Guid PolicyId, string? PoliceReportNumber);

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
        "ACCEPTED",
        NextStepAllowed: true);
}

public sealed record FieldError(string Field, string Code, string Message);

public sealed record ValidationErrorResponse(string Code, IReadOnlyList<FieldError> FieldErrors);

/// <summary>Thrown when a theft claim request fails field-level validation (AISDLC-51
/// contract) — mapped to a 422 response by the controller, distinct from
/// <see cref="PolicyPlatform.Domain.Common.DomainException"/> which maps to 400.</summary>
public sealed class TheftClaimValidationException : Exception
{
    public IReadOnlyList<FieldError> FieldErrors { get; }

    public TheftClaimValidationException(IReadOnlyList<FieldError> fieldErrors)
        : base("Theft claim validation failed.")
        => FieldErrors = fieldErrors;
}
