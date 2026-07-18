using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class InMemoryClaimLastPaidTrancheViewRepositoryTests
{
    private static ClaimLastPaidTrancheViewRecord MakeRecord(Guid claimId, decimal grossAmount = 100m) =>
        new(claimId, Guid.NewGuid(), 1, "PAID", DateTimeOffset.UtcNow, grossAmount, "PLN",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact]
    public async Task GetAsync_NoRowForClaim_ReturnsNull()
    {
        var repo = new InMemoryClaimLastPaidTrancheViewRepository();

        var result = await repo.GetAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertAsync_ThenGetAsync_ReturnsStoredRecordForThatClaim()
    {
        var repo = new InMemoryClaimLastPaidTrancheViewRepository();
        var claimId = Guid.NewGuid();
        var record = MakeRecord(claimId);

        await repo.UpsertAsync(record);
        var result = await repo.GetAsync(claimId);

        Assert.Equal(record, result);
    }

    [Fact]
    public async Task UpsertAsync_CalledTwiceForSameClaim_OverwritesPreviousRow()
    {
        var repo = new InMemoryClaimLastPaidTrancheViewRepository();
        var claimId = Guid.NewGuid();
        await repo.UpsertAsync(MakeRecord(claimId, grossAmount: 100m));

        await repo.UpsertAsync(MakeRecord(claimId, grossAmount: 200m));
        var result = await repo.GetAsync(claimId);

        Assert.Equal(200m, result!.GrossAmount);
    }

    [Fact]
    public async Task UpsertAsync_DifferentClaims_AreStoredIndependently()
    {
        var repo = new InMemoryClaimLastPaidTrancheViewRepository();
        var claimIdA = Guid.NewGuid();
        var claimIdB = Guid.NewGuid();
        await repo.UpsertAsync(MakeRecord(claimIdA, grossAmount: 100m));
        await repo.UpsertAsync(MakeRecord(claimIdB, grossAmount: 200m));

        var resultA = await repo.GetAsync(claimIdA);
        var resultB = await repo.GetAsync(claimIdB);

        Assert.Equal(100m, resultA!.GrossAmount);
        Assert.Equal(200m, resultB!.GrossAmount);
    }
}
