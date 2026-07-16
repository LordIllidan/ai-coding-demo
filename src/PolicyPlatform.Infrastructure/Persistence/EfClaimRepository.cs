using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class EfClaimRepository : IClaimRepository
{
    private readonly PolicyPlatformDbContext _db;

    public EfClaimRepository(PolicyPlatformDbContext db) => _db = db;

    public async Task<TheftClaim?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.TheftClaims.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(TheftClaim claim, CancellationToken ct = default)
    {
        await _db.TheftClaims.AddAsync(claim, ct);
        await _db.SaveChangesAsync(ct);
    }
}
