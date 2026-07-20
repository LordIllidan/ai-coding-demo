using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.LoginHistory;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>In-memory <see cref="ILoginHistoryRepository"/> implementation, used when no SQL connection string is configured.</summary>
public sealed class InMemoryLoginHistoryRepository : ILoginHistoryRepository
{
    private readonly ConcurrentDictionary<Guid, LoginHistoryEntry> _entries = new();

    /// <inheritdoc />
    public Task<IReadOnlyList<LoginHistoryEntry>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        IReadOnlyList<LoginHistoryEntry> result = _entries.Values
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.OccurredAt)
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default)
    {
        _entries[entry.Id] = entry;
        return Task.CompletedTask;
    }
}
