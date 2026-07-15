using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class EfPolicyRepository : IPolicyRepository
{
    private readonly PolicyPlatformDbContext _db;

    public EfPolicyRepository(PolicyPlatformDbContext db) => _db = db;

    public async Task<Policy?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Policies.Include(p => p.Coverages).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Policy?> GetByNumberAsync(PolicyNumber number, CancellationToken ct = default)
        => await _db.Policies.Include(p => p.Coverages).FirstOrDefaultAsync(p => p.Number.Value == number.Value, ct);

    public async Task<IReadOnlyList<Policy>> ListAsync(CancellationToken ct = default)
        => await _db.Policies.Include(p => p.Coverages).ToListAsync(ct);

    public async Task AddAsync(Policy policy, CancellationToken ct = default)
    {
        await _db.Policies.AddAsync(policy, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Policy policy, CancellationToken ct = default)
    {
        // Policy was already fetched (and is therefore tracked) via GetByIdAsync in this
        // same DbContext scope, so this flushes whatever mutations were made to it.
        await _db.SaveChangesAsync(ct);
    }
}
