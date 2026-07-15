using PolicyPlatform.Application.Customers;
using PolicyPlatform.Application.Notifications;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Notifications;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class DeviceRegistrationServiceTests
{
    private static (DeviceRegistrationService Devices, CustomerService Customers) CreateServices()
    {
        var customerRepo = new InMemoryCustomerRepository();
        var deviceRepo = new InMemoryDeviceRegistrationRepository();
        return (new DeviceRegistrationService(deviceRepo, customerRepo), new CustomerService(customerRepo));
    }

    [Fact]
    public async Task RegisterDeviceAsync_UnknownCustomer_Throws()
    {
        var (devices, _) = CreateServices();
        var request = new RegisterDeviceRequest(Guid.NewGuid(), "token", DevicePlatform.Ios, true);

        await Assert.ThrowsAsync<DomainException>(() => devices.RegisterDeviceAsync(request));
    }

    [Fact]
    public async Task RegisterDeviceAsync_KnownCustomer_ReturnsActiveDeviceDto()
    {
        var (devices, customers) = CreateServices();
        var customer = await customers.CreateCustomerAsync(new CreateCustomerRequest("Jan Kowalski", "jan@example.com"));

        var device = await devices.RegisterDeviceAsync(
            new RegisterDeviceRequest(customer.Id, "token-123", DevicePlatform.Android, true));

        Assert.Equal(customer.Id, device.CustomerId);
        Assert.Equal("Android", device.Platform);
        Assert.True(device.NotificationsPermissionGranted);
        Assert.True(device.IsActive);
    }

    [Fact]
    public async Task UnregisterDeviceAsync_UnknownDevice_Throws()
    {
        var (devices, _) = CreateServices();

        await Assert.ThrowsAsync<DomainException>(() => devices.UnregisterDeviceAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UnregisterDeviceAsync_KnownDevice_MarksInactive()
    {
        var (devices, customers) = CreateServices();
        var customer = await customers.CreateCustomerAsync(new CreateCustomerRequest("Anna Nowak", "anna@example.com"));
        var registered = await devices.RegisterDeviceAsync(
            new RegisterDeviceRequest(customer.Id, "token-456", DevicePlatform.Ios, true));

        var unregistered = await devices.UnregisterDeviceAsync(registered.Id);

        Assert.False(unregistered.IsActive);
    }
}
