using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Abstractions;

public interface IClaimRepository
{
    Task<Claim?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Claim>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Claim claim, CancellationToken ct = default);
}
