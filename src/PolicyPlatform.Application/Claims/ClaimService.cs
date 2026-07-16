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

    /// <summary>Validates and registers a theft claim against an existing policy.</summary>
    /// <param name="request">Policy id and raw police report number to validate/normalize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created claim as a 201 response payload.</returns>
    /// <exception cref="PoliceReportNumberValidationException">
    /// <paramref name="request"/>'s police report number is missing or fails the format check.</exception>
    /// <exception cref="DomainException">No policy exists for <paramref name="request"/>'s policy id.</exception>
    public async Task<TheftClaimCreatedResponse> RegisterTheftClaimAsync(
        CreateTheftClaimRequest request, CancellationToken ct = default)
    {
        // Validate the police report number before touching the DB — an invalid
        // format is a client error (422) regardless of whether the policy exists.
        var policeReportNumber = new PoliceReportNumber(request.PoliceReportNumber);

        _ = await _policies.GetByIdAsync(request.PolicyId, ct)
            ?? throw new DomainException($"Policy {request.PolicyId} was not found.");

        var claim = TheftClaim.Register(Guid.NewGuid(), request.PolicyId, policeReportNumber, DateTime.UtcNow);

        await _claims.AddAsync(claim, ct);
        return TheftClaimCreatedResponse.FromDomain(claim);
    }

    /// <summary>Looks up a theft claim by id.</summary>
    /// <param name="claimId">Id of the claim to fetch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The claim DTO, or <see langword="null"/> if no claim exists with that id.</returns>
    public async Task<TheftClaimDto?> GetTheftClaimAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await _claims.GetByIdAsync(claimId, ct);
        return claim is null ? null : TheftClaimDto.FromDomain(claim);
    }
}
