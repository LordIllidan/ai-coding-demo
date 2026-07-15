using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Abstractions;

public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Policy?> GetByNumberAsync(PolicyNumber number, CancellationToken ct = default);
    Task<IReadOnlyList<Policy>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Policy policy, CancellationToken ct = default);
}
