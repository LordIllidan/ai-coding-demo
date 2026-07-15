using PolicyPlatform.Application.Notifications;

namespace PolicyPlatform.Application.Abstractions;

public interface IPushNotificationSender
{
    Task SendAsync(DeviceToken target, PushNotification notification, CancellationToken ct = default);
}
