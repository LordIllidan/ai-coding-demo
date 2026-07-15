using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Policies;

namespace PolicyPlatform.Application.Notifications;

/// <summary>Wraps <see cref="PolicyService"/> status transitions with a push notification
/// to the customer's registered devices. Kept separate from PolicyService so the core
/// policy use-cases stay free of notification concerns.</summary>
public sealed class PolicyStatusNotificationService
{
    private readonly PolicyService _policies;
    private readonly IDeviceRegistrationRepository _devices;
    private readonly IPushNotificationSender _sender;

    public PolicyStatusNotificationService(
        PolicyService policies, IDeviceRegistrationRepository devices, IPushNotificationSender sender)
    {
        _policies = policies;
        _devices = devices;
        _sender = sender;
    }

    public async Task<PolicyDto> ActivatePolicyAsync(Guid policyId, CancellationToken ct = default)
    {
        var policy = await _policies.ActivatePolicyAsync(policyId, ct);
        await NotifyStatusChangeAsync(policy, ct);
        return policy;
    }

    public async Task<PolicyDto> CancelPolicyAsync(Guid policyId, CancellationToken ct = default)
    {
        var policy = await _policies.CancelPolicyAsync(policyId, ct);
        await NotifyStatusChangeAsync(policy, ct);
        return policy;
    }

    private async Task NotifyStatusChangeAsync(PolicyDto policy, CancellationToken ct)
    {
        var devices = await _devices.ListActiveByCustomerIdAsync(policy.CustomerId, ct);
        foreach (var device in devices)
        {
            await _sender.SendAsync(
                device,
                title: "Status zgłoszenia zaktualizowany",
                body: $"Polisa {policy.Number} ma nowy status: {policy.Status}.",
                ct);
        }
    }
}
