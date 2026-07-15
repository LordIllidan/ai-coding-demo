using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Notifications;

/// <summary>Sends a push notification to every device registered for a customer whenever
/// one of their policies changes status.</summary>
public sealed class PolicyStatusPushDispatcher
{
    private readonly IDeviceTokenRepository _deviceTokens;
    private readonly IPushNotificationSender _sender;

    public PolicyStatusPushDispatcher(IDeviceTokenRepository deviceTokens, IPushNotificationSender sender)
    {
        _deviceTokens = deviceTokens;
        _sender = sender;
    }

    public async Task DispatchAsync(Policy policy, CancellationToken ct = default)
    {
        var notification = PolicyStatusNotificationMapper.Map(policy.Number, policy.Status);
        if (notification is null)
        {
            return;
        }

        var tokens = await _deviceTokens.GetByCustomerAsync(policy.CustomerId, ct);
        foreach (var token in tokens)
        {
            await _sender.SendAsync(token, notification, ct);
        }
    }
}
