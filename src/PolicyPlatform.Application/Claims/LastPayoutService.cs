using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Application.Claims;

/// <summary>Use-case for GET /api/mobile/me/claims/last-payout. The customer identity comes
/// exclusively from the caller's JWT (via ICustomerIdentityResolver) — the request itself
/// carries no customerId, policyId, or claimId.</summary>
public sealed class LastPayoutService
{
    private readonly ICustomerIdentityResolver _identity;
    private readonly ILastPayoutRepository _repository;

    public LastPayoutService(ICustomerIdentityResolver identity, ILastPayoutRepository repository)
    {
        _identity = identity;
        _repository = repository;
    }

    public async Task<LastPayoutResponse> GetLastPayoutAsync(string? authorizationHeaderValue, CancellationToken ct = default)
    {
        var customerId = _identity.ResolveCustomerId(authorizationHeaderValue);

        var record = await _repository.GetLastPayoutAsync(customerId, ct)
            ?? throw new LastPayoutNotFoundException(customerId);

        return LastPayoutResponse.FromRecord(record);
    }
}
