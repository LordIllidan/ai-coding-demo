namespace PolicyPlatform.Application.Claims;

/// <summary>Authorization header missing, malformed, or the token is expired/unparseable.</summary>
public sealed class AuthRequiredException() : Exception("Access token is missing or invalid.");

/// <summary>Token is structurally valid but not scoped to the caller's own customer data.</summary>
public sealed class ForbiddenCrossCustomerException() : Exception("Token is not authorized for this customer's data.");

/// <summary>No PAID claim_payout row exists for the resolved customer.</summary>
public sealed class LastPayoutNotFoundException(Guid customerId) : Exception($"No paid payout found for customer {customerId}.")
{
    public Guid CustomerId { get; } = customerId;
}

/// <summary>The payout data source timed out or is unreachable. Must never be swallowed in
/// favor of returning stale data.</summary>
public sealed class DataSourceTimeoutException() : Exception("The payout data source timed out.");
