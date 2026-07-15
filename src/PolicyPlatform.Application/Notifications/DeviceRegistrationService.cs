using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Application.Notifications;

/// <summary>Application service (use-case layer) for registering/unregistering the mobile
/// device tokens that push notifications about zgłoszenie status changes are sent to.</summary>
public sealed class DeviceRegistrationService
{
    private readonly IDeviceTokenRepository _deviceTokens;
    private readonly ICustomerRepository _customers;

    public DeviceRegistrationService(IDeviceTokenRepository deviceTokens, ICustomerRepository customers)
    {
        _deviceTokens = deviceTokens;
        _customers = customers;
    }

    public async Task RegisterAsync(DeviceRegistrationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new DomainException("Device token must not be empty.");
        }

        _ = await _customers.GetByIdAsync(request.CustomerId, ct)
            ?? throw new DomainException($"Customer {request.CustomerId} was not found.");

        await _deviceTokens.RegisterAsync(
            new DeviceToken(request.CustomerId, request.Token, request.Platform, DateTimeOffset.UtcNow), ct);
    }

    public Task UnregisterAsync(string token, CancellationToken ct = default)
        => _deviceTokens.UnregisterAsync(token, ct);
}
