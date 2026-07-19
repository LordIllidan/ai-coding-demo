using PolicyPlatform.Domain.Notifications;

namespace PolicyPlatform.Application.Notifications;

public sealed record NotificationCounterDto(int UnreadCount, DateTime CalculatedAt);

public sealed record NotificationListItemDto(
    Guid Id, string Title, string Body, string Type, DateTime CreatedAt, bool IsRead, DateTime? ReadAt)
{
    public static NotificationListItemDto FromDomain(Notification notification) => new(
        notification.Id,
        notification.Title,
        notification.Body,
        notification.Type,
        notification.CreatedAt,
        notification.IsRead,
        notification.ReadAt);
}

public sealed record NotificationListDto(IReadOnlyList<NotificationListItemDto> Items, string? NextCursor);

public sealed record NotificationReadResultDto(Guid NotificationId, bool IsRead, DateTime ReadAt, int UnreadCount);
