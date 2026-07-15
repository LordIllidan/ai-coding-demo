using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Abstractions;

public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Policy?> GetByNumberAsync(PolicyNumber number, CancellationToken ct = default);
    Task<IReadOnlyList<Policy>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Policy policy, CancellationToken ct = default);

    /// <summary>Persists mutations made to a policy already returned by this repository
    /// (e.g. after Activate()/Cancel()). Required because a tracking ORM (EF Core) will not
    /// otherwise flush in-memory mutations to storage — the in-memory implementation treats
    /// this as a no-op since it shares object identity with its backing store.</summary>
    Task UpdateAsync(Policy policy, CancellationToken ct = default);
}
