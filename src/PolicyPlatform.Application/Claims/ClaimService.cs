using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Application.Claims;

/// <summary>Application service (use-case layer). Orchestrates domain objects and
/// repositories; contains no business rules itself — those live in the Domain.</summary>
public sealed class ClaimService
{
    private readonly IClaimRepository _claims;
    private readonly IPolicyRepository _policies;

    public ClaimService(IClaimRepository claims, IPolicyRepository policies)
    {
        _claims = claims;
        _policies = policies;
    }

    /// <summary>Validates and registers a new theft claim: validates the police report number
    /// first (AISDLC-51 contract), then confirms the referenced policy exists.</summary>
    /// <param name="request">The claim request, including raw policy id and police report number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registered claim.</returns>
    /// <exception cref="TheftClaimValidationException">Thrown when <see cref="CreateTheftClaimRequest.PoliceReportNumber"/> is missing or invalid.</exception>
    /// <exception cref="DomainException">Thrown when the referenced policy does not exist.</exception>
    public async Task<TheftClaimDto> RegisterTheftClaimAsync(
        CreateTheftClaimRequest request, CancellationToken ct = default)
    {
        if (!PoliceReportNumber.TryCreate(request.PoliceReportNumber, out var policeReportNumber, out var error))
        {
            var code = error == PoliceReportNumberError.Required
                ? "POLICE_REPORT_NUMBER_REQUIRED"
                : "POLICE_REPORT_NUMBER_INVALID_FORMAT";
            throw new TheftClaimValidationException([
                new FieldError("policeReportNumber", code, "Numer zgłoszenia Policji jest wymagany i musi być poprawny.")
            ]);
        }

        _ = await _policies.GetByIdAsync(request.PolicyId, ct)
            ?? throw new DomainException($"Policy {request.PolicyId} was not found.");

        var claim = TheftClaim.Register(Guid.NewGuid(), request.PolicyId, policeReportNumber, DateTime.UtcNow);

        await _claims.AddAsync(claim, ct);
        return TheftClaimDto.FromDomain(claim);
    }

    /// <summary>Retrieves a previously registered theft claim by id.</summary>
    /// <param name="claimId">Identifier of the claim.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The claim, or <c>null</c> if no claim with that id exists.</returns>
    public async Task<TheftClaimDto?> GetTheftClaimAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await _claims.GetByIdAsync(claimId, ct);
        return claim is null ? null : TheftClaimDto.FromDomain(claim);
    }
}
