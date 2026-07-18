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

    /// <inheritdoc />
    public Task<ClaimPayout?> GetLastPaidForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        var last = _payouts.Values
            .Where(p => p.CustomerId == customerId && p.Status == ClaimPayoutStatus.Paid)
            .OrderByDescending(p => p.PaidAt)
            .FirstOrDefault();

        return Task.FromResult(last);
    }

    /// <summary>Inserts or replaces a payout record, keyed by its id.</summary>
    /// <param name="payout">The payout to store.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task AddAsync(ClaimPayout payout, CancellationToken ct = default)
    {
        _payouts[payout.Id] = payout;
        return Task.CompletedTask;
    }
}
