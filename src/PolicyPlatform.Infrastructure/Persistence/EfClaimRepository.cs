using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class EfClaimRepository : IClaimRepository
{
    private readonly PolicyPlatformDbContext _db;

    public EfClaimRepository(PolicyPlatformDbContext db) => _db = db;

    public async Task<Claim?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Claims.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Claim>> ListAsync(CancellationToken ct = default)
        => await _db.Claims.ToListAsync(ct);

    public async Task AddAsync(Claim claim, CancellationToken ct = default)
    {
        _db.Claims.Add(claim);
        await _db.SaveChangesAsync(ct);
    }
}
