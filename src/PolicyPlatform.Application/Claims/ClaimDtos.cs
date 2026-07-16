using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

/// <summary>Request body for <c>POST /api/theft-claims</c> (AISDLC-51 contract).</summary>
/// <param name="PolicyId">Identifier of the policy the claim is filed against.</param>
/// <param name="PoliceReportNumber">Raw, unvalidated police report number as submitted by the caller.</param>
public sealed record CreateTheftClaimRequest(Guid PolicyId, string? PoliceReportNumber);

/// <summary>Response body for a registered or retrieved theft claim (AISDLC-51 contract).</summary>
/// <param name="ClaimId">Identifier of the claim.</param>
/// <param name="PolicyId">Identifier of the policy the claim is filed against.</param>
/// <param name="PoliceReportNumber">Normalized (UPPERCASE) police report number.</param>
/// <param name="Status">Claim status; currently always <c>"ACCEPTED"</c>.</param>
/// <param name="NextStepAllowed">Whether the process may continue to the next step.</param>
public sealed record TheftClaimDto(
    Guid ClaimId,
    Guid PolicyId,
    string PoliceReportNumber,
    string Status,
    bool NextStepAllowed)
{
    /// <summary>Maps a <see cref="TheftClaim"/> domain entity to its API representation.</summary>
    /// <param name="claim">The domain entity to map.</param>
    /// <returns>The corresponding <see cref="TheftClaimDto"/>.</returns>
    public static TheftClaimDto FromDomain(TheftClaim claim) => new(
        claim.Id,
        claim.PolicyId,
        claim.PoliceReportNumber.Value,
        "ACCEPTED",
        NextStepAllowed: true);
}

/// <summary>A single field-level validation failure (AISDLC-51 422 error contract).</summary>
/// <param name="Field">Name of the invalid request field, e.g. <c>"policeReportNumber"</c>.</param>
/// <param name="Code">Machine-readable error code, e.g. <c>"POLICE_REPORT_NUMBER_REQUIRED"</c>.</param>
/// <param name="Message">Human-readable message describing the failure.</param>
public sealed record FieldError(string Field, string Code, string Message);

/// <summary>Body of a <c>422 Unprocessable Entity</c> response (AISDLC-51 contract).</summary>
/// <param name="Code">Top-level error code; currently always <c>"VALIDATION_ERROR"</c>.</param>
/// <param name="FieldErrors">The individual field validation failures.</param>
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
