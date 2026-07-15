using PolicyPlatform.Application.Notifications;

namespace PolicyPlatform.Application.Abstractions;

public interface IDeviceTokenRepository
{
    Task RegisterAsync(DeviceToken token, CancellationToken ct = default);

    Task UnregisterAsync(string token, CancellationToken ct = default);

    Task<IReadOnlyList<DeviceToken>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
}
