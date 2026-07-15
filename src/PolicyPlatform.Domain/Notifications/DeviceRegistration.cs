using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Notifications;

public sealed class DeviceRegistration : Entity
{
    public Guid CustomerId { get; }
    public string PushToken { get; }
    public DevicePlatform Platform { get; }
    public bool NotificationsPermissionGranted { get; }
    public bool IsActive { get; private set; }

    private DeviceRegistration(
        Guid id, Guid customerId, string pushToken, DevicePlatform platform, bool notificationsPermissionGranted)
        : base(id)
    {
        CustomerId = customerId;
        PushToken = pushToken;
        Platform = platform;
        NotificationsPermissionGranted = notificationsPermissionGranted;
        IsActive = true;
    }

    public static DeviceRegistration Register(
        Guid id, Guid customerId, string pushToken, DevicePlatform platform, bool notificationsPermissionGranted)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Device registration must belong to a valid customer.");
        }

        if (string.IsNullOrWhiteSpace(pushToken))
        {
            throw new DomainException("Push token is required to register a device.");
        }

        if (!notificationsPermissionGranted)
        {
            throw new DomainException(
                "Device cannot be registered for push notifications without OS-level notification permission.");
        }

        return new DeviceRegistration(id, customerId, pushToken.Trim(), platform, notificationsPermissionGranted);
    }

    public void Revoke()
    {
        IsActive = false;
    }
}
