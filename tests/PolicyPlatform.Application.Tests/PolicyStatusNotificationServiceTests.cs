using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Customers;
using PolicyPlatform.Application.Notifications;
using PolicyPlatform.Application.Policies;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Notifications;
using PolicyPlatform.Domain.Policies;
using PolicyPlatform.Infrastructure.Numbering;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class PolicyStatusNotificationServiceTests
{
    private sealed class RecordingPushNotificationSender : IPushNotificationSender
    {
        public List<(DeviceRegistration Device, string Title, string Body)> Sent { get; } = [];

        public Task SendAsync(DeviceRegistration device, string title, string body, CancellationToken ct = default)
        {
            Sent.Add((device, title, body));
            return Task.CompletedTask;
        }
    }

    private static (
        PolicyStatusNotificationService Notifications,
        CustomerService Customers,
        PolicyService Policies,
        DeviceRegistrationService Devices,
        RecordingPushNotificationSender Sender) CreateServices()
    {
        var customerRepo = new InMemoryCustomerRepository();
        var policyRepo = new InMemoryPolicyRepository();
        var deviceRepo = new InMemoryDeviceRegistrationRepository();
        var sender = new RecordingPushNotificationSender();
        var policyService = new PolicyService(policyRepo, customerRepo, new SequentialPolicyNumberGenerator());
        return (
            new PolicyStatusNotificationService(policyService, deviceRepo, sender),
            new CustomerService(customerRepo),
            policyService,
            new DeviceRegistrationService(deviceRepo, customerRepo),
            sender);
    }

    private static async Task<PolicyDto> CreateActivatablePolicyAsync(PolicyService policies, Guid customerId)
        => await policies.CreatePolicyAsync(new CreatePolicyRequest(
            customerId, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            [new CoverageRequest(CoverageType.OC, 50000, 800)]));

    [Fact]
    public async Task ActivatePolicyAsync_WithActiveDevice_SendsPushNotification()
    {
        var (notifications, customers, policies, devices, sender) = CreateServices();
        var customer = await customers.CreateCustomerAsync(new CreateCustomerRequest("Jan Kowalski", "jan@example.com"));
        await devices.RegisterDeviceAsync(new RegisterDeviceRequest(customer.Id, "token", DevicePlatform.Ios, true));
        var policy = await CreateActivatablePolicyAsync(policies, customer.Id);

        var activated = await notifications.ActivatePolicyAsync(policy.Id);

        Assert.Equal("Active", activated.Status);
        var notification = Assert.Single(sender.Sent);
        Assert.Contains(policy.Number, notification.Body);
        Assert.Contains("Active", notification.Body);
    }

    [Fact]
    public async Task CancelPolicyAsync_WithActiveDevice_SendsPushNotification()
    {
        var (notifications, customers, policies, devices, sender) = CreateServices();
        var customer = await customers.CreateCustomerAsync(new CreateCustomerRequest("Anna Nowak", "anna@example.com"));
        await devices.RegisterDeviceAsync(new RegisterDeviceRequest(customer.Id, "token", DevicePlatform.Android, true));
        var policy = await CreateActivatablePolicyAsync(policies, customer.Id);
        await notifications.ActivatePolicyAsync(policy.Id);
        sender.Sent.Clear();

        var cancelled = await notifications.CancelPolicyAsync(policy.Id);

        Assert.Equal("Cancelled", cancelled.Status);
        var notification = Assert.Single(sender.Sent);
        Assert.Contains("Cancelled", notification.Body);
    }

    [Fact]
    public async Task ActivatePolicyAsync_NoRegisteredDevices_SendsNoNotification()
    {
        var (notifications, customers, policies, _, sender) = CreateServices();
        var customer = await customers.CreateCustomerAsync(new CreateCustomerRequest("Ewa Wisniewska", "ewa@example.com"));
        var policy = await CreateActivatablePolicyAsync(policies, customer.Id);

        await notifications.ActivatePolicyAsync(policy.Id);

        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task ActivatePolicyAsync_WithRevokedDevice_SendsNoNotification()
    {
        var (notifications, customers, policies, devices, sender) = CreateServices();
        var customer = await customers.CreateCustomerAsync(new CreateCustomerRequest("Piotr Zielinski", "piotr@example.com"));
        var device = await devices.RegisterDeviceAsync(
            new RegisterDeviceRequest(customer.Id, "token", DevicePlatform.Ios, true));
        await devices.UnregisterDeviceAsync(device.Id);
        var policy = await CreateActivatablePolicyAsync(policies, customer.Id);

        await notifications.ActivatePolicyAsync(policy.Id);

        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task ActivatePolicyAsync_UnknownPolicy_Throws()
    {
        var (notifications, _, _, _, _) = CreateServices();

        await Assert.ThrowsAsync<DomainException>(() => notifications.ActivatePolicyAsync(Guid.NewGuid()));
    }
}
