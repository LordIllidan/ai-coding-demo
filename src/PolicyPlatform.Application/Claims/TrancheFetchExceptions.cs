namespace PolicyPlatform.Application.Claims;

/// <summary>Authorization header missing, malformed, or the token has expired.</summary>
public sealed class InvalidTokenException() : Exception("Access token is missing or invalid.");

/// <summary>Token is valid but does not carry a scope for the requested claim.</summary>
public sealed class ClaimAccessDeniedException(Guid claimId) : Exception($"Access to claim {claimId} is denied.")
{
    public Guid ClaimId { get; } = claimId;
}

public sealed class ClaimNotFoundException(Guid claimId) : Exception($"Claim {claimId} was not found.")
{
    public Guid ClaimId { get; } = claimId;
}

/// <summary>Downstream tranche integration is unreachable or its circuit breaker is open.</summary>
public sealed class TrancheServiceUnavailableException() : Exception("Tranche service is unavailable.");

/// <summary>Downstream tranche integration did not respond within the configured timeout.</summary>
public sealed class TrancheServiceTimeoutException() : Exception("Tranche service request timed out.");
