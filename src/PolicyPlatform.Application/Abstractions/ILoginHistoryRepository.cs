using PolicyPlatform.Domain.Auth;

namespace PolicyPlatform.Application.Abstractions;

public interface ILoginHistoryRepository
{
    /// <summary>Returns entries for the given user, sorted by OccurredAt descending
    /// (newest first) to match the mobile login-history contract.</summary>
    Task<IReadOnlyList<LoginHistoryEntry>> ListForUserAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default);
}
