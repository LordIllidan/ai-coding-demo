using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime stand-in for the claim_last_paid_tranche_view read model.
/// Swap for an EF Core-backed view once claims move to durable persistence.</summary>
public sealed class InMemoryClaimLastPaidTrancheViewRepository : IClaimLastPaidTrancheViewRepository
{
    private readonly ConcurrentDictionary<Guid, ClaimLastPaidTrancheViewRecord> _rows = new();

    /// <inheritdoc />
    public Task<ClaimLastPaidTrancheViewRecord?> GetAsync(Guid claimId, CancellationToken ct = default)
        => Task.FromResult(_rows.GetValueOrDefault(claimId));

    /// <inheritdoc />
    public Task UpsertAsync(ClaimLastPaidTrancheViewRecord record, CancellationToken ct = default)
    {
        _rows[record.ClaimId] = record;
        return Task.CompletedTask;
    }
}
