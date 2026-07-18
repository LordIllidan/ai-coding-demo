using System.Data.Common;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Mobile;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Policies;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class MobileClaimPayoutServiceTests
{
    private sealed class FakeDbException(string message) : DbException(message);

    private sealed class ThrowingClaimPayoutRepository(Exception toThrow) : IClaimPayoutRepository
    {
        public Task<ClaimPayout?> GetLastPaidPayoutAsync(Guid customerId, CancellationToken ct = default)
            => throw toThrow;
    }

    private static ClaimPayout CreatePayout(Guid customerId, DateTime paidAt, ClaimPayoutStatus status = ClaimPayoutStatus.Paid) =>
        ClaimPayout.Create(
            Guid.NewGuid(), Guid.NewGuid(), "SZK/1/2026", customerId,
            new Money(1234.5m, "PLN"), paidAt, status);

    [Fact]
    public async Task GetLastPayoutAsync_NoPayoutForCustomer_ThrowsNotFound()
    {
        var service = new MobileClaimPayoutService(new InMemoryClaimPayoutRepository());

        await Assert.ThrowsAsync<LastPayoutNotFoundException>(
            () => service.GetLastPayoutAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetLastPayoutAsync_PayoutExists_ReturnsMappedResponse()
    {
        var repo = new InMemoryClaimPayoutRepository();
        var customerId = Guid.NewGuid();
        var payout = CreatePayout(customerId, new DateTime(2026, 6, 15));
        await repo.AddAsync(payout);
        var service = new MobileClaimPayoutService(repo);

        var response = await service.GetLastPayoutAsync(customerId);

        Assert.Equal(payout.ClaimNumber, response.ClaimNumber);
        Assert.Equal("1234.50", response.Amount.Value);
        Assert.Equal("PLN", response.Amount.Currency);
        Assert.Equal("2026-06-15", response.PayoutDate);
        Assert.True(response.ReadOnly);
    }

    [Fact]
    public async Task GetLastPayoutAsync_MultiplePayouts_ReturnsMostRecentlyPaid()
    {
        var repo = new InMemoryClaimPayoutRepository();
        var customerId = Guid.NewGuid();
        var older = CreatePayout(customerId, new DateTime(2026, 1, 1));
        var newer = CreatePayout(customerId, new DateTime(2026, 6, 1));
        await repo.AddAsync(older);
        await repo.AddAsync(newer);
        var service = new MobileClaimPayoutService(repo);

        var response = await service.GetLastPayoutAsync(customerId);

        Assert.Equal(newer.ClaimNumber, response.ClaimNumber);
    }

    [Fact]
    public async Task GetLastPayoutAsync_OnlyUnpaidPayoutExists_ThrowsNotFound()
    {
        var repo = new InMemoryClaimPayoutRepository();
        var customerId = Guid.NewGuid();
        await repo.AddAsync(CreatePayout(customerId, new DateTime(2026, 1, 1), ClaimPayoutStatus.Pending));
        var service = new MobileClaimPayoutService(repo);

        await Assert.ThrowsAsync<LastPayoutNotFoundException>(
            () => service.GetLastPayoutAsync(customerId));
    }

    [Fact]
    public async Task GetLastPayoutAsync_OtherCustomersPayoutOnly_ThrowsNotFound()
    {
        var repo = new InMemoryClaimPayoutRepository();
        await repo.AddAsync(CreatePayout(Guid.NewGuid(), new DateTime(2026, 1, 1)));
        var service = new MobileClaimPayoutService(repo);

        await Assert.ThrowsAsync<LastPayoutNotFoundException>(
            () => service.GetLastPayoutAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetLastPayoutAsync_RepositoryTimesOut_ThrowsDataSourceUnavailable()
    {
        var service = new MobileClaimPayoutService(new ThrowingClaimPayoutRepository(new TimeoutException()));

        await Assert.ThrowsAsync<DataSourceUnavailableException>(
            () => service.GetLastPayoutAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetLastPayoutAsync_RepositoryThrowsDbException_ThrowsDataSourceUnavailable()
    {
        var service = new MobileClaimPayoutService(new ThrowingClaimPayoutRepository(new FakeDbException("boom")));

        await Assert.ThrowsAsync<DataSourceUnavailableException>(
            () => service.GetLastPayoutAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetLastPayoutAsync_RepositoryThrowsUnrelatedException_PropagatesUnwrapped()
    {
        var service = new MobileClaimPayoutService(new ThrowingClaimPayoutRepository(new InvalidOperationException("boom")));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetLastPayoutAsync(Guid.NewGuid()));
    }
}
