using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Infrastructure.Security;

/// <summary>Checks that a bearer token is present. Scope-to-claimId enforcement (403
/// CLAIM_ACCESS_DENIED) requires a real IdP/token-introspection integration and is not yet
/// wired up — this validator only ever throws InvalidTokenException, leaving the
/// ClaimAccessDeniedException path ready for that follow-up work.</summary>
public sealed class BearerClaimAccessValidator : IClaimAccessValidator
{
    private const string BearerPrefix = "Bearer ";

    public Task EnsureAccessAsync(string? authorizationHeaderValue, Guid claimId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeaderValue) ||
            !authorizationHeaderValue.StartsWith(BearerPrefix, StringComparison.Ordinal) ||
            authorizationHeaderValue.Length <= BearerPrefix.Length)
        {
            throw new InvalidTokenException();
        }

        return Task.CompletedTask;
    }
}
