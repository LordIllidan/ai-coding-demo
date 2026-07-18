using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime in-memory store. Swap for an EF Core provider once claim
/// payouts need durable persistence — the Application layer only depends on
/// IClaimPayoutRepository.</summary>
public sealed class InMemoryClaimPayoutRepository : IClaimPayoutRepository
{
    private readonly ConcurrentDictionary<Guid, ClaimPayout> _payouts = new();

    public Task<ClaimPayout?> GetLastPaidForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var last = _payouts.Values
            .Where(p => p.CustomerId == customerId && p.Status == ClaimPayoutStatus.Paid)
            .OrderByDescending(p => p.PaidAt)
            .FirstOrDefault();

        return Task.FromResult(last);
    }

    public Task AddAsync(ClaimPayout payout, CancellationToken ct = default)
    {
        _payouts[payout.Id] = payout;
        return Task.CompletedTask;
    }
}
