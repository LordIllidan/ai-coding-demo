using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Application.Notifications;

/// <summary>Application service (use-case layer) for the mobile unread-notifications
/// counter flow. Every method takes the current user's id resolved from the JWT — callers
/// never pass a target user id, so cross-account access is structurally impossible except
/// via the read-notification path, which is checked explicitly.</summary>
public sealed class NotificationService
{
    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications) => _notifications = notifications;

    public async Task<NotificationCounterDto> GetCounterAsync(Guid userId, CancellationToken ct = default)
    {
        var unreadCount = await _notifications.CountUnreadAsync(userId, ct);
        return new NotificationCounterDto(unreadCount, DateTime.UtcNow);
    }

    public async Task<NotificationListDto> GetUnreadAsync(
        Guid userId, int limit, string? cursor, CancellationToken ct = default)
    {
        var (items, nextCursor) = await _notifications.GetUnreadAsync(userId, limit, cursor, ct);
        return new NotificationListDto(items.Select(NotificationListItemDto.FromDomain).ToList(), nextCursor);
    }

    public async Task<NotificationReadResultDto> MarkAsReadAsync(
        Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _notifications.GetByIdAsync(notificationId, ct)
            ?? throw new NotificationAccessException(NotificationAccessError.NotFound);

        if (notification.UserId != userId)
        {
            throw new NotificationAccessException(NotificationAccessError.Forbidden);
        }

        notification.MarkAsRead(DateTime.UtcNow);
        await _notifications.SaveAsync(notification, ct);

        var unreadCount = await _notifications.CountUnreadAsync(userId, ct);
        return new NotificationReadResultDto(notification.Id, notification.IsRead, notification.ReadAt!.Value, unreadCount);
    }
}
