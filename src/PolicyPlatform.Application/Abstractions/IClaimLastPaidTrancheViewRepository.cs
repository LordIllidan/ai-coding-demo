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

public interface IClaimLastPaidTrancheViewRepository
{
    Task<ClaimLastPaidTrancheViewRecord?> GetAsync(Guid claimId, CancellationToken ct = default);

    Task UpsertAsync(ClaimLastPaidTrancheViewRecord record, CancellationToken ct = default);
}
