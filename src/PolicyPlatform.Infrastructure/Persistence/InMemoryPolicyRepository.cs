using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime in-memory store. Swap for an EF Core provider once a real
/// database is provisioned — the Application layer only depends on IPolicyRepository.</summary>
public sealed class InMemoryPolicyRepository : IPolicyRepository
{
    private readonly ConcurrentDictionary<Guid, Policy> _policies = new();

    public Task<Policy?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_policies.GetValueOrDefault(id));

    public Task<Policy?> GetByNumberAsync(PolicyNumber number, CancellationToken ct = default)
        => Task.FromResult(_policies.Values.FirstOrDefault(p => p.Number.Value == number.Value));

    public Task<IReadOnlyList<Policy>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Policy>>(_policies.Values.ToList());

    public Task AddAsync(Policy policy, CancellationToken ct = default)
    {
        _policies[policy.Id] = policy;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Policy policy, CancellationToken ct = default)
    {
        // No-op: GetByIdAsync returns the same object reference stored in _policies, so
        // mutations (Activate()/Cancel()) are already reflected. Present only to satisfy
        // IPolicyRepository symmetrically with EfPolicyRepository, which needs a real flush.
        return Task.CompletedTask;
    }
}
