using PolicyPlatform.Domain.Notifications;

namespace PolicyPlatform.Application.Abstractions;

public interface IPushNotificationSender
{
    Task SendAsync(DeviceRegistration device, string title, string body, CancellationToken ct = default);
}
