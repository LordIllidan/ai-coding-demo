using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class InMemoryLastPayoutRepositoryTests
{
    [Fact]
    public async Task GetLastPayoutAsync_UnknownCustomer_ReturnsNull()
    {
        var repository = new InMemoryLastPayoutRepository();

        var result = await repository.GetLastPayoutAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastPayoutAsync_OnlyNonPaidPayouts_ReturnsNull()
    {
        var repository = new InMemoryLastPayoutRepository();
        var customerId = Guid.NewGuid();
        repository.Seed(customerId, new CustomerPayout("PL-1", 100m, "PLN", DateTimeOffset.UtcNow, "PENDING"));
        repository.Seed(customerId, new CustomerPayout("PL-2", 200m, "PLN", DateTimeOffset.UtcNow, "CANCELLED"));

        var result = await repository.GetLastPayoutAsync(customerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastPayoutAsync_SinglePaidPayout_ReturnsMappedRecord()
    {
        var repository = new InMemoryLastPayoutRepository();
        var customerId = Guid.NewGuid();
        var paidAt = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
        repository.Seed(customerId, new CustomerPayout("PL-2002", 3250.5m, "PLN", paidAt, "PAID"));

        var result = await repository.GetLastPayoutAsync(customerId);

        Assert.NotNull(result);
        Assert.Equal("PL-2002", result!.ClaimNumber);
        Assert.Equal(3250.5m, result.AmountGross);
        Assert.Equal("PLN", result.CurrencyCode);
        Assert.Equal(new DateOnly(2026, 3, 10), result.PayoutDate);
    }

    [Fact]
    public async Task GetLastPayoutAsync_MultiplePaidPayouts_ReturnsLatestByPaidAt()
    {
        var repository = new InMemoryLastPayoutRepository();
        var customerId = Guid.NewGuid();
        repository.Seed(customerId, new CustomerPayout("PL-OLD", 100m, "PLN", DateTimeOffset.Parse("2025-01-01T00:00:00Z"), "PAID"));
        repository.Seed(customerId, new CustomerPayout("PL-NEW", 200m, "PLN", DateTimeOffset.Parse("2026-05-01T00:00:00Z"), "PAID"));
        repository.Seed(customerId, new CustomerPayout("PL-MID", 150m, "PLN", DateTimeOffset.Parse("2025-06-01T00:00:00Z"), "PAID"));

        var result = await repository.GetLastPayoutAsync(customerId);

        Assert.Equal("PL-NEW", result!.ClaimNumber);
    }

    [Fact]
    public async Task GetLastPayoutAsync_IgnoresNonPaidRowsEvenWhenMoreRecent()
    {
        var repository = new InMemoryLastPayoutRepository();
        var customerId = Guid.NewGuid();
        repository.Seed(customerId, new CustomerPayout("PL-PAID", 100m, "PLN", DateTimeOffset.Parse("2025-01-01T00:00:00Z"), "PAID"));
        repository.Seed(customerId, new CustomerPayout("PL-PENDING", 999m, "PLN", DateTimeOffset.Parse("2026-06-01T00:00:00Z"), "PENDING"));

        var result = await repository.GetLastPayoutAsync(customerId);

        Assert.Equal("PL-PAID", result!.ClaimNumber);
    }

    [Fact]
    public async Task GetLastPayoutAsync_TwoCustomers_EachOnlySeesOwnSeededPayout()
    {
        var repository = new InMemoryLastPayoutRepository();
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();
        repository.Seed(customerA, new CustomerPayout("PL-A", 111.11m, "PLN", DateTimeOffset.UtcNow.AddDays(-2), "PAID"));
        repository.Seed(customerB, new CustomerPayout("PL-B", 222.22m, "PLN", DateTimeOffset.UtcNow.AddDays(-1), "PAID"));

        var resultA = await repository.GetLastPayoutAsync(customerA);
        var resultB = await repository.GetLastPayoutAsync(customerB);

        Assert.Equal("PL-A", resultA!.ClaimNumber);
        Assert.Equal("PL-B", resultB!.ClaimNumber);
    }
}
