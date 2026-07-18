using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>In-memory <see cref="IClaimPayoutRepository"/> used when no
/// "PolicyPlatformDb" connection string is configured, so local dev/demo runs with
/// zero external dependencies.</summary>
public sealed class InMemoryClaimPayoutRepository : IClaimPayoutRepository
{
    private readonly ConcurrentDictionary<Guid, ClaimPayout> _payouts = new();

    /// <inheritdoc/>
    public Task<ClaimPayout?> GetLastPaidPayoutAsync(Guid customerId, CancellationToken ct = default)
    {
        var latest = _payouts.Values
            .Where(p => p.CustomerId == customerId && p.Status == ClaimPayoutStatus.Paid)
            .OrderByDescending(p => p.PaidAt)
            .FirstOrDefault();
        return Task.FromResult(latest);
    }

    /// <summary>Adds or replaces a payout record. Test/seed helper — not part of
    /// <see cref="IClaimPayoutRepository"/>.</summary>
    /// <param name="payout">The payout to store.</param>
    /// <param name="ct">Cancellation token (unused; in-memory store completes synchronously).</param>
    public Task AddAsync(ClaimPayout payout, CancellationToken ct = default)
    {
        _payouts[payout.Id] = payout;
        return Task.CompletedTask;
    }
}
