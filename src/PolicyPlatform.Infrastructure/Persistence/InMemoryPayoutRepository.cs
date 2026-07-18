using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime in-memory store — no write/ingestion path exists yet, so this
/// always resolves to NO_PAYOUT until a real payouts table (and an EF Core repository
/// querying it with claims.id = claimId, ORDER BY paid_at DESC, installment_no DESC) exists.</summary>
public sealed class InMemoryPayoutRepository : IPayoutRepository
{
    private readonly ConcurrentDictionary<Guid, List<PayoutRecord>> _payoutsByClaimId = new();

    public Task<PayoutRecord?> GetLastPaidInstallmentAsync(Guid claimId, CancellationToken ct = default)
    {
        if (!_payoutsByClaimId.TryGetValue(claimId, out var payouts) || payouts.Count == 0)
        {
            return Task.FromResult<PayoutRecord?>(null);
        }

        var last = payouts
            .OrderByDescending(p => p.PaidAt ?? DateOnly.MinValue)
            .ThenByDescending(p => p.InstallmentNo ?? 0)
            .First();

        return Task.FromResult<PayoutRecord?>(last);
    }
}
