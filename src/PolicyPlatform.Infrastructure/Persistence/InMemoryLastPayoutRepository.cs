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

    /// <summary>Reads the last PAID payout seeded for the given customer, if any.</summary>
    /// <param name="customerId">Customer id resolved from the caller's JWT.</param>
    /// <param name="ct">Cancellation token (unused; the in-memory store never awaits).</param>
    /// <returns>The last paid payout record, or <c>null</c> when the customer has none.</returns>
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

    /// <summary>Adds a payout row for a customer. Test/bootstrap seeding only — not part of
    /// <see cref="ILastPayoutRepository"/>.</summary>
    /// <param name="customerId">Customer the payout belongs to.</param>
    /// <param name="payout">Payout row to add.</param>
    public void Seed(Guid customerId, CustomerPayout payout)
        => _payoutsByCustomer.GetOrAdd(customerId, static _ => []).Add(payout);
}

/// <summary>Row shape mirroring the shared claim_payout columns (amount_gross, currency_code,
/// paid_at, status) plus the joined claim_number, for the in-memory stand-in only.</summary>
/// <param name="ClaimNumber">Human-readable claim number, joined from the claim.</param>
/// <param name="AmountGross">Gross payout amount.</param>
/// <param name="CurrencyCode">3-letter ISO currency code.</param>
/// <param name="PaidAt">Timestamp the payout was paid.</param>
/// <param name="Status">Payout status; only rows with status "PAID" are eligible.</param>
public sealed record CustomerPayout(
    string ClaimNumber,
    decimal AmountGross,
    string CurrencyCode,
    DateTimeOffset PaidAt,
    string Status);
