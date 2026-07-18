using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class ClaimPayoutServiceTests
{
    private static (ClaimPayoutService Service, InMemoryClaimPayoutRepository Repository) CreateService()
    {
        var repository = new InMemoryClaimPayoutRepository();
        return (new ClaimPayoutService(repository), repository);
    }

    private static ClaimPayout CreatePayout(
        Guid customerId, string claimNumber, decimal amountGross, DateTime paidAt,
        ClaimPayoutStatus status = ClaimPayoutStatus.Paid) => ClaimPayout.Register(
        Guid.NewGuid(), Guid.NewGuid(), claimNumber, customerId, amountGross, "PLN", paidAt, status);

    [Fact]
    public async Task GetLastPayoutForCustomer_NoPayouts_ReturnsNull()
    {
        var (service, _) = CreateService();

        var result = await service.GetLastPayoutForCustomerAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastPayoutForCustomer_OnlyOtherCustomersPayout_ReturnsNull()
    {
        var (service, repository) = CreateService();
        await repository.AddAsync(CreatePayout(
            Guid.NewGuid(), "CLM-OTHER", 100m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await service.GetLastPayoutForCustomerAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastPayoutForCustomer_OnlyRejectedPayout_ReturnsNull()
    {
        var (service, repository) = CreateService();
        var customerId = Guid.NewGuid();
        await repository.AddAsync(CreatePayout(
            customerId, "CLM-REJ", 100m, DateTime.UtcNow, ClaimPayoutStatus.Rejected));

        var result = await service.GetLastPayoutForCustomerAsync(customerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastPayoutForCustomer_MultiplePayouts_ReturnsMostRecentMapped()
    {
        var (service, repository) = CreateService();
        var customerId = Guid.NewGuid();
        await repository.AddAsync(CreatePayout(
            customerId, "CLM-OLD", 100m, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await repository.AddAsync(CreatePayout(
            customerId, "CLM-NEW", 1234.5m, new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc)));

        var result = await service.GetLastPayoutForCustomerAsync(customerId);

        Assert.NotNull(result);
        Assert.Equal("CLM-NEW", result!.ClaimNumber);
        Assert.Equal("1234.50", result.Amount.Value);
        Assert.Equal("PLN", result.Amount.Currency);
        Assert.Equal("2026-03-10", result.PayoutDate);
        Assert.True(result.ReadOnly);
    }
}
