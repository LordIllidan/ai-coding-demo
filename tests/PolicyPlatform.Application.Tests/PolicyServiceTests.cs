using PolicyPlatform.Application.Customers;
using PolicyPlatform.Application.Policies;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Policies;
using PolicyPlatform.Infrastructure.Numbering;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class PolicyServiceTests
{
    private static (PolicyService Policies, CustomerService Customers) CreateServices()
    {
        var customerRepo = new InMemoryCustomerRepository();
        var policyRepo = new InMemoryPolicyRepository();
        var numberGenerator = new SequentialPolicyNumberGenerator();
        return (
            new PolicyService(policyRepo, customerRepo, numberGenerator),
            new CustomerService(customerRepo));
    }

    [Fact]
    public async Task CreatePolicy_UnknownCustomer_Throws()
    {
        var (policies, _) = CreateServices();
        var request = new CreatePolicyRequest(
            Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            [new CoverageRequest(CoverageType.OC, 50000, 800)]);

        await Assert.ThrowsAsync<DomainException>(() => policies.CreatePolicyAsync(request));
    }

    [Fact]
    public async Task CreatePolicy_KnownCustomer_ReturnsDraftPolicy()
    {
        var (policies, customers) = CreateServices();
        var customer = await customers.CreateCustomerAsync(new CreateCustomerRequest("Jan Kowalski", "jan@example.com"));

        var policy = await policies.CreatePolicyAsync(new CreatePolicyRequest(
            customer.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            [new CoverageRequest(CoverageType.OC, 50000, 800)]));

        Assert.Equal("Draft", policy.Status);
        Assert.StartsWith("POL-", policy.Number);
        Assert.Equal(800m, policy.TotalPremium);
    }

    [Fact]
    public async Task ActivatePolicy_RoundTrip_UpdatesStoredStatus()
    {
        var (policies, customers) = CreateServices();
        var customer = await customers.CreateCustomerAsync(new CreateCustomerRequest("Anna Nowak", "anna@example.com"));
        var created = await policies.CreatePolicyAsync(new CreatePolicyRequest(
            customer.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            [new CoverageRequest(CoverageType.OC, 50000, 800)]));

        var activated = await policies.ActivatePolicyAsync(created.Id);
        var fetched = await policies.GetPolicyAsync(created.Id);

        Assert.Equal("Active", activated.Status);
        Assert.Equal("Active", fetched!.Status);
    }

    [Fact]
    public async Task GetPolicy_UnknownId_ReturnsNull()
    {
        var (policies, _) = CreateServices();

        var result = await policies.GetPolicyAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ListPolicies_ReturnsAllCreated()
    {
        var (policies, customers) = CreateServices();
        var customer = await customers.CreateCustomerAsync(new CreateCustomerRequest("Ewa Wisniewska", "ewa@example.com"));
        await policies.CreatePolicyAsync(new CreatePolicyRequest(
            customer.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            [new CoverageRequest(CoverageType.OC, 50000, 800)]));
        await policies.CreatePolicyAsync(new CreatePolicyRequest(
            customer.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            [new CoverageRequest(CoverageType.OC, 60000, 900)]));

        var all = await policies.ListPoliciesAsync();

        Assert.Equal(2, all.Count);
    }
}
