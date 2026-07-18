using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Application.Claims;

/// <summary>Use-case for GET /api/claims/{claimId}/last-paid-tranche. Uses claimId only —
/// customerId/policyId never enter the lookup or authorization logic.</summary>
public sealed class ClaimLastPaidTrancheService
{
    private readonly IClaimRepository _claims;
    private readonly IClaimAccessValidator _accessValidator;
    private readonly ITrancheIntegrationClient _trancheClient;
    private readonly IClaimLastPaidTrancheViewRepository _view;
    private readonly TimeProvider _clock;

    public ClaimLastPaidTrancheService(
        IClaimRepository claims,
        IClaimAccessValidator accessValidator,
        ITrancheIntegrationClient trancheClient,
        IClaimLastPaidTrancheViewRepository view,
        TimeProvider? clock = null)
    {
        _claims = claims;
        _accessValidator = accessValidator;
        _trancheClient = trancheClient;
        _view = view;
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>Fetches the last paid tranche for a claim, validating access first and
    /// never falling back to a previously cached view row when the downstream fetch fails.</summary>
    /// <param name="claimId">Claim identifier (UUID). The sole lookup/authorization key.</param>
    /// <param name="authorizationHeaderValue">Raw value of the caller's <c>Authorization</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The claim id, the last paid tranche (or <see langword="null"/> if none), and the fetch timestamp.</returns>
    /// <exception cref="InvalidTokenException">Token is missing, malformed, or expired.</exception>
    /// <exception cref="ClaimAccessDeniedException">Token is valid but out of scope for <paramref name="claimId"/>.</exception>
    /// <exception cref="ClaimNotFoundException">No claim exists for <paramref name="claimId"/>.</exception>
    /// <exception cref="TrancheServiceUnavailableException">The downstream tranche service is unreachable or its circuit breaker is open.</exception>
    /// <exception cref="TrancheServiceTimeoutException">The downstream tranche call did not complete within the configured timeout.</exception>
    public async Task<LastPaidTrancheResult> GetLastPaidTrancheAsync(
        Guid claimId, string? authorizationHeaderValue, CancellationToken ct = default)
    {
        await _accessValidator.EnsureAccessAsync(authorizationHeaderValue, claimId, ct);

        var claim = await _claims.GetByIdAsync(claimId, ct)
            ?? throw new ClaimNotFoundException(claimId);

        // No try/catch around this call by design: on timeout or an open circuit breaker the
        // client throws and that error must propagate as-is, never masked by a fallback read
        // of the (possibly stale) claim_last_paid_tranche_view row.
        var tranche = await _trancheClient.GetLastPaidTrancheAsync(claimId, ct);

        var fetchedAt = _clock.GetUtcNow();

        if (tranche is not null)
        {
            await _view.UpsertAsync(
                new ClaimLastPaidTrancheViewRecord(
                    claimId,
                    tranche.TrancheId,
                    tranche.TrancheNumber,
                    tranche.Status,
                    tranche.PaidAt,
                    tranche.GrossAmount,
                    tranche.Currency,
                    tranche.PaidAt,
                    fetchedAt),
                ct);
        }

        return new LastPaidTrancheResult(claimId, tranche, fetchedAt);
    }
}
