using Microsoft.Extensions.Logging;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Notifications;

namespace PolicyPlatform.Infrastructure.Notifications;

/// <summary>Placeholder IPushNotificationSender: logs instead of calling FCM/APNs.
/// Swap for a real gateway (FCM HTTP v1 for Android, APNs for iOS) once push
/// credentials are provisioned — the Application layer only depends on IPushNotificationSender.</summary>
public sealed class LoggingPushNotificationSender : IPushNotificationSender
{
    private readonly ILogger<LoggingPushNotificationSender> _logger;

    public LoggingPushNotificationSender(ILogger<LoggingPushNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(DeviceRegistration device, string title, string body, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Push notification to device {DeviceId} ({Platform}) for customer {CustomerId}: {Title} — {Body}",
            device.Id, device.Platform, device.CustomerId, title, body);
        return Task.CompletedTask;
    }
}
