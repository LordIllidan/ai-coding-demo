using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime in-memory stand-in for the claim_payout table. Swap for an EF
/// Core provider once claim payouts need durable persistence — the Application layer only
/// depends on ILastPayoutRepository.</summary>
public sealed class InMemoryLastPayoutRepository : ILastPayoutRepository
{
    private readonly ConcurrentDictionary<Guid, List<CustomerPayout>> _payoutsByCustomer = new();

    public Task<LastPayoutRecord?> GetLastPayoutAsync(Guid customerId, CancellationToken ct = default)
    {
        if (!_payoutsByCustomer.TryGetValue(customerId, out var payouts))
        {
            return Task.FromResult<LastPayoutRecord?>(null);
        }

        var last = payouts
            .Where(p => p.Status == "PAID")
            .OrderByDescending(p => p.PaidAt)
            .FirstOrDefault();

        return Task.FromResult(last is null
            ? null
            : new LastPayoutRecord(last.ClaimNumber, last.AmountGross, last.CurrencyCode, DateOnly.FromDateTime(last.PaidAt.UtcDateTime)));
    }

    public void Seed(Guid customerId, CustomerPayout payout)
        => _payoutsByCustomer.GetOrAdd(customerId, static _ => []).Add(payout);
}

/// <summary>Row shape mirroring the shared claim_payout columns (amount_gross, currency_code,
/// paid_at, status) plus the joined claim_number, for the in-memory stand-in only.</summary>
public sealed record CustomerPayout(
    string ClaimNumber,
    decimal AmountGross,
    string CurrencyCode,
    DateTimeOffset PaidAt,
    string Status);
