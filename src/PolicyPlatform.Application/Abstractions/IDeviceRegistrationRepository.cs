using PolicyPlatform.Domain.Notifications;

namespace PolicyPlatform.Application.Abstractions;

public interface IDeviceRegistrationRepository
{
    Task<DeviceRegistration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceRegistration>> ListActiveByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(DeviceRegistration device, CancellationToken ct = default);
}
