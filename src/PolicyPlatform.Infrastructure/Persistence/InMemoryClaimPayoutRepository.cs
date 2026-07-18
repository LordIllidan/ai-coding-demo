using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class InMemoryClaimPayoutRepository : IClaimPayoutRepository
{
    private readonly ConcurrentDictionary<Guid, ClaimPayout> _payouts = new();

    public Task<ClaimPayout?> GetLastPaidPayoutAsync(Guid customerId, CancellationToken ct = default)
    {
        var latest = _payouts.Values
            .Where(p => p.CustomerId == customerId && p.Status == ClaimPayoutStatus.Paid)
            .OrderByDescending(p => p.PaidAt)
            .FirstOrDefault();
        return Task.FromResult(latest);
    }

    public Task AddAsync(ClaimPayout payout, CancellationToken ct = default)
    {
        _payouts[payout.Id] = payout;
        return Task.CompletedTask;
    }
}
