using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Notifications;

namespace PolicyPlatform.Infrastructure.Notifications;

/// <summary>Push handler used until a real FCM/APNs provider is wired up: it logs the
/// notification instead of delivering it, so the rest of the pipeline (registration,
/// status mapping, dispatch) can be exercised end-to-end.</summary>
public sealed class LoggingPushNotificationSender : IPushNotificationSender
{
    private readonly ILogger<LoggingPushNotificationSender> _logger;
    private readonly PushNotificationOptions _options;

    public LoggingPushNotificationSender(
        ILogger<LoggingPushNotificationSender> logger, IOptions<PushNotificationOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task SendAsync(DeviceToken target, PushNotification notification, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Push [{Provider}] -> {Platform} device {Token}: {Title} - {Body}",
            _options.Provider, target.Platform, target.Token, notification.Title, notification.Body);

        return Task.CompletedTask;
    }
}
