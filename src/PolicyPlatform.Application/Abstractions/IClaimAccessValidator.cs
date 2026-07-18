namespace PolicyPlatform.Application.Abstractions;

/// <summary>Validates the caller's bearer token and its scope for a given claim.
/// Throws PolicyPlatform.Application.Claims.InvalidTokenException (401) or
/// ClaimAccessDeniedException (403) when access must be refused.</summary>
public interface IClaimAccessValidator
{
    /// <summary>Validates the bearer token and its scope for <paramref name="claimId"/>.</summary>
    /// <param name="authorizationHeaderValue">Raw value of the <c>Authorization</c> header.</param>
    /// <param name="claimId">Claim the caller is trying to access.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="PolicyPlatform.Application.Claims.InvalidTokenException">Token is missing, malformed, or expired.</exception>
    /// <exception cref="PolicyPlatform.Application.Claims.ClaimAccessDeniedException">Token is valid but out of scope for <paramref name="claimId"/>.</exception>
    Task EnsureAccessAsync(string? authorizationHeaderValue, Guid claimId, CancellationToken ct = default);
}
