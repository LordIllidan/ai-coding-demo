using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Auth;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class EfLoginHistoryRepository : ILoginHistoryRepository
{
    private readonly PolicyPlatformDbContext _db;

    public EfLoginHistoryRepository(PolicyPlatformDbContext db) => _db = db;

    public async Task<IReadOnlyList<LoginHistoryEntry>> ListForUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.LoginHistoryEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);

    public async Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default)
    {
        await _db.LoginHistoryEntries.AddAsync(entry, ct);
        await _db.SaveChangesAsync(ct);
    }
}
