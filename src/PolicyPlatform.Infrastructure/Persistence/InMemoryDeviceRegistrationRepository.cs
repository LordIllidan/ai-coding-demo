using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Notifications;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime in-memory store. Swap for an EF Core provider once a real
/// database is provisioned — the Application layer only depends on IDeviceRegistrationRepository.</summary>
public sealed class InMemoryDeviceRegistrationRepository : IDeviceRegistrationRepository
{
    private readonly ConcurrentDictionary<Guid, DeviceRegistration> _devices = new();

    public Task<DeviceRegistration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_devices.GetValueOrDefault(id));

    public Task<IReadOnlyList<DeviceRegistration>> ListActiveByCustomerIdAsync(
        Guid customerId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DeviceRegistration>>(
            _devices.Values.Where(d => d.CustomerId == customerId && d.IsActive).ToList());

    public Task AddAsync(DeviceRegistration device, CancellationToken ct = default)
    {
        _devices[device.Id] = device;
        return Task.CompletedTask;
    }
}
