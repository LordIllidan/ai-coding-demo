using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime in-memory store. Swap for an EF Core provider once a real
/// database is provisioned — the Application layer only depends on IClaimRepository.</summary>
public sealed class InMemoryClaimRepository : IClaimRepository
{
    private readonly ConcurrentDictionary<Guid, Claim> _claims = new();

    public Task<Claim?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_claims.GetValueOrDefault(id));

    public Task<IReadOnlyList<Claim>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Claim>>(_claims.Values.ToList());

    public Task AddAsync(Claim claim, CancellationToken ct = default)
    {
        _claims[claim.Id] = claim;
        return Task.CompletedTask;
    }
}
