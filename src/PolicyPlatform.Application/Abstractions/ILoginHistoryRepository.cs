using PolicyPlatform.Domain.LoginHistory;

namespace PolicyPlatform.Application.Abstractions;

public interface ILoginHistoryRepository
{
    /// <summary>Returns the given user's login history, newest entry first.</summary>
    Task<IReadOnlyList<LoginHistoryEntry>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default);
}
