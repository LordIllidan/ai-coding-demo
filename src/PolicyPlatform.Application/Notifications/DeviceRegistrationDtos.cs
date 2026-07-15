using PolicyPlatform.Domain.Notifications;

namespace PolicyPlatform.Application.Notifications;

public sealed record RegisterDeviceRequest(
    Guid CustomerId,
    string PushToken,
    DevicePlatform Platform,
    bool NotificationsPermissionGranted);

public sealed record DeviceRegistrationDto(
    Guid Id,
    Guid CustomerId,
    string Platform,
    bool NotificationsPermissionGranted,
    bool IsActive)
{
    public static DeviceRegistrationDto FromDomain(DeviceRegistration device) => new(
        device.Id,
        device.CustomerId,
        device.Platform.ToString(),
        device.NotificationsPermissionGranted,
        device.IsActive);
}
