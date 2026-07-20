using PolicyPlatform.Domain.LoginHistory;

namespace PolicyPlatform.Application.Abstractions;

/// <summary>Persists and reads back <see cref="LoginHistoryEntry"/> records.</summary>
public interface ILoginHistoryRepository
{
    /// <summary>Returns the given user's login history, newest entry first.</summary>
    /// <param name="userId">Id of the user whose login history to fetch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user's login history entries ordered by <c>OccurredAt</c> descending.</returns>
    Task<IReadOnlyList<LoginHistoryEntry>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Adds a new login history entry.</summary>
    /// <param name="entry">The entry to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default);
}
