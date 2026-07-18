using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Application.Claims;

/// <summary>Use-case for GET /api/mobile/me/claims/last-payout. The customer identity comes
/// exclusively from the caller's JWT (via ICustomerIdentityResolver) — the request itself
/// carries no customerId, policyId, or claimId.</summary>
public sealed class LastPayoutService
{
    private readonly ICustomerIdentityResolver _identity;
    private readonly ILastPayoutRepository _repository;

    /// <summary>Creates the service with its identity resolver and payout repository dependencies.</summary>
    /// <param name="identity">Resolves the caller's customer id from the request's bearer token.</param>
    /// <param name="repository">Read-only access to the last PAID payout for a customer.</param>
    public LastPayoutService(ICustomerIdentityResolver identity, ILastPayoutRepository repository)
    {
        _identity = identity;
        _repository = repository;
    }

    /// <summary>Resolves the caller's identity from the token and returns their last paid payout.</summary>
    /// <param name="authorizationHeaderValue">Raw value of the incoming Authorization header.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mobile last-payout response for the authenticated customer.</returns>
    /// <exception cref="AuthRequiredException">The token is missing, malformed, or expired.</exception>
    /// <exception cref="ForbiddenCrossCustomerException">The token is not scoped to the caller's own data.</exception>
    /// <exception cref="LastPayoutNotFoundException">No PAID payout exists for the customer.</exception>
    /// <exception cref="DataSourceTimeoutException">The payout data source timed out or is unreachable.</exception>
    public async Task<LastPayoutResponse> GetLastPayoutAsync(string? authorizationHeaderValue, CancellationToken ct = default)
    {
        var customerId = _identity.ResolveCustomerId(authorizationHeaderValue);

        var record = await _repository.GetLastPayoutAsync(customerId, ct)
            ?? throw new LastPayoutNotFoundException(customerId);

        return LastPayoutResponse.FromRecord(record);
    }
}
