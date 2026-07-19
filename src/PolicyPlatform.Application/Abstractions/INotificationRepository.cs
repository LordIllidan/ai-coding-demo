using PolicyPlatform.Domain.Notifications;

namespace PolicyPlatform.Application.Abstractions;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Unread notifications for the user, newest first, paginated with an opaque cursor.
    /// Returns up to <paramref name="limit"/> items plus the cursor for the next page (null when exhausted).</summary>
    Task<(IReadOnlyList<Notification> Items, string? NextCursor)> GetUnreadAsync(
        Guid userId, int limit, string? cursor, CancellationToken ct = default);

    Task SaveAsync(Notification notification, CancellationToken ct = default);
}
