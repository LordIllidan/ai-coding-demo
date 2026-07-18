namespace PolicyPlatform.Application.Abstractions;

/// <summary>Validates the caller's bearer token and its scope for a given claim.
/// Throws PolicyPlatform.Application.Claims.InvalidTokenException (401) or
/// ClaimAccessDeniedException (403) when access must be refused.</summary>
public interface IClaimAccessValidator
{
    Task EnsureAccessAsync(string? authorizationHeaderValue, Guid claimId, CancellationToken ct = default);
}
