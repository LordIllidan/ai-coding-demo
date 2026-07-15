using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

public sealed record InitiateClaimRequest(
    Guid PolicyId,
    Guid CustomerId,
    string Channel,
    DateOnly IncidentDate,
    string? Description);

public sealed record ClaimDto(
    Guid Id,
    Guid PolicyId,
    Guid CustomerId,
    string Channel,
    DateOnly IncidentDate,
    string? Description,
    DateTime CreatedAtUtc)
{
    public static ClaimDto FromDomain(Claim claim) => new(
        claim.Id,
        claim.PolicyId,
        claim.CustomerId,
        claim.Channel.ToString(),
        claim.IncidentDate,
        claim.Description,
        claim.CreatedAtUtc);
}
