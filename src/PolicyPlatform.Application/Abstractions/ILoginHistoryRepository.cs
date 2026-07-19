using PolicyPlatform.Domain.Identity;

namespace PolicyPlatform.Application.Abstractions;

public interface ILoginHistoryRepository
{
    Task<IReadOnlyList<LoginHistoryEntry>> ListForUserAsync(Guid userId, CancellationToken ct = default);
}
