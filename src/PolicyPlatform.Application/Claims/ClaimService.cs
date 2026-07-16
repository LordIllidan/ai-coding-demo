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

    public async Task<TheftClaimDto?> GetTheftClaimAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await _claims.GetByIdAsync(claimId, ct);
        return claim is null ? null : TheftClaimDto.FromDomain(claim);
    }
}
