using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Abstractions;

public interface IClaimRepository
{
    Task<TheftClaim?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(TheftClaim claim, CancellationToken ct = default);
}
