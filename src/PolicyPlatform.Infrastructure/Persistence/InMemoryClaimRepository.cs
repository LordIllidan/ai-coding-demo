using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime in-memory store. Swap for an EF Core provider once claims
/// need durable persistence — the Application layer only depends on IClaimRepository.</summary>
public sealed class InMemoryClaimRepository : IClaimRepository
{
    private readonly ConcurrentDictionary<Guid, TheftClaim> _claims = new();

    public Task<TheftClaim?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_claims.GetValueOrDefault(id));

    public Task AddAsync(TheftClaim claim, CancellationToken ct = default)
    {
        _claims[claim.Id] = claim;
        return Task.CompletedTask;
    }
}
