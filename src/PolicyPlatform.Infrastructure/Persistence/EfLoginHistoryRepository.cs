using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.LoginHistory;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>EF Core-backed <see cref="ILoginHistoryRepository"/> implementation.</summary>
public sealed class EfLoginHistoryRepository : ILoginHistoryRepository
{
    private readonly PolicyPlatformDbContext _db;

    /// <summary>Creates a new <see cref="EfLoginHistoryRepository"/>.</summary>
    /// <param name="db">The EF Core database context.</param>
    public EfLoginHistoryRepository(PolicyPlatformDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<LoginHistoryEntry>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.LoginHistoryEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default)
    {
        await _db.LoginHistoryEntries.AddAsync(entry, ct);
        await _db.SaveChangesAsync(ct);
    }
}
