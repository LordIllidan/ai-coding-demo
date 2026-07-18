namespace PolicyPlatform.Application.Abstractions;

/// <summary>Read model backing claim_last_paid_tranche_view. Keyed by claim_id only —
/// never by customer_id/policy_id. Refreshed on a successful downstream fetch; must not
/// be consulted to serve a response after a failed fetch (see ITrancheIntegrationClient).</summary>
public sealed record ClaimLastPaidTrancheViewRecord(
    Guid ClaimId,
    Guid TrancheId,
    int TrancheNumber,
    string Status,
    DateTimeOffset PaidAt,
    decimal GrossAmount,
    string Currency,
    DateTimeOffset SourceUpdatedAt,
    DateTimeOffset RefreshedAt);

/// <summary>Repository for the claim_last_paid_tranche_view read model.</summary>
public interface IClaimLastPaidTrancheViewRepository
{
    /// <summary>Reads the cached view row for a claim, if one has been refreshed before.</summary>
    /// <param name="claimId">Claim identifier (the view's primary key).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The row, or <see langword="null"/> when the view has never been refreshed for this claim.</returns>
    Task<ClaimLastPaidTrancheViewRecord?> GetAsync(Guid claimId, CancellationToken ct = default);

    /// <summary>Inserts or replaces the view row for <paramref name="record"/>'s claim.</summary>
    /// <param name="record">Row to persist, keyed by its <see cref="ClaimLastPaidTrancheViewRecord.ClaimId"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(ClaimLastPaidTrancheViewRecord record, CancellationToken ct = default);
}
