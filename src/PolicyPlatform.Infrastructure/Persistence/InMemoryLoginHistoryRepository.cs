using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Identity;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Local dev/demo fallback used when no SQL Server connection string is configured.
/// Login history rows are written by the mobile login flow (a separate piece of work), so
/// this store starts empty and simply satisfies the read contract for /me/login-history.</summary>
public sealed class InMemoryLoginHistoryRepository : ILoginHistoryRepository
{
    private readonly ConcurrentDictionary<Guid, List<LoginHistoryEntry>> _entriesByUser = new();

    public Task<IReadOnlyList<LoginHistoryEntry>> ListForUserAsync(Guid userId, CancellationToken ct = default)
    {
        IReadOnlyList<LoginHistoryEntry> result = _entriesByUser.TryGetValue(userId, out var entries)
            ? entries.OrderByDescending(e => e.OccurredAt).ToList()
            : [];

        return Task.FromResult(result);
    }
}
