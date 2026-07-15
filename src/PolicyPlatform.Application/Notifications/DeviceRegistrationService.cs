using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Notifications;

namespace PolicyPlatform.Application.Notifications;

/// <summary>Application service (use-case layer). Orchestrates domain objects and
/// repositories; contains no business rules itself — those live in the Domain.</summary>
public sealed class DeviceRegistrationService
{
    private readonly IDeviceRegistrationRepository _devices;
    private readonly ICustomerRepository _customers;

    public DeviceRegistrationService(IDeviceRegistrationRepository devices, ICustomerRepository customers)
    {
        _devices = devices;
        _customers = customers;
    }

    public async Task<DeviceRegistrationDto> RegisterDeviceAsync(
        RegisterDeviceRequest request, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, ct)
            ?? throw new DomainException($"Customer {request.CustomerId} was not found.");

        var device = DeviceRegistration.Register(
            Guid.NewGuid(), customer.Id, request.PushToken, request.Platform, request.NotificationsPermissionGranted);

        await _devices.AddAsync(device, ct);
        return DeviceRegistrationDto.FromDomain(device);
    }

    public async Task<DeviceRegistrationDto> UnregisterDeviceAsync(Guid deviceId, CancellationToken ct = default)
    {
        var device = await _devices.GetByIdAsync(deviceId, ct)
            ?? throw new DomainException($"Device registration {deviceId} was not found.");

        device.Revoke();
        return DeviceRegistrationDto.FromDomain(device);
    }
}
