using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class PayoutServiceTests
{
    private sealed class FakePayoutRepository : IPayoutRepository
    {
        private readonly PayoutRecord? _record;

        public FakePayoutRepository(PayoutRecord? record) => _record = record;

        public Task<PayoutRecord?> GetLastPaidInstallmentAsync(Guid claimId, CancellationToken ct = default)
            => Task.FromResult(_record);
    }

    private static async Task<(TheftClaim Claim, InMemoryClaimRepository Claims)> CreateClaimAsync()
    {
        var claims = new InMemoryClaimRepository();
        var claim = TheftClaim.Register(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            "Kradziez pojazdu.",
            new PoliceReportNumber("KMP/123/2026"),
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        await claims.AddAsync(claim);
        return (claim, claims);
    }

    [Fact]
    public async Task GetLastPaidInstallmentAsync_UnknownClaim_ReturnsNull()
    {
        var claims = new InMemoryClaimRepository();
        var service = new PayoutService(claims, new FakePayoutRepository(null));

        var result = await service.GetLastPaidInstallmentAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastPaidInstallmentAsync_KnownClaimNoPayoutRecord_ReturnsNoPayout()
    {
        var (claim, claims) = await CreateClaimAsync();
        var service = new PayoutService(claims, new FakePayoutRepository(null));

        var result = await service.GetLastPaidInstallmentAsync(claim.Id);

        Assert.NotNull(result);
        Assert.Equal(claim.Id, result!.ClaimId);
        Assert.Equal(PayoutScreenStates.NoPayout, result.ScreenState);
        Assert.Null(result.LastPaidInstallment);
        Assert.False(result.CanEdit);
        Assert.Null(result.ClaimNumber);
    }

    [Fact]
    public async Task GetLastPaidInstallmentAsync_CompleteRecord_ReturnsPaidWithClaimNumber()
    {
        var (claim, claims) = await CreateClaimAsync();
        var record = new PayoutRecord(Guid.NewGuid(), 2, new DateOnly(2026, 3, 1), 1234.56m, "pln");
        var service = new PayoutService(claims, new FakePayoutRepository(record));

        var result = await service.GetLastPaidInstallmentAsync(claim.Id);

        Assert.NotNull(result);
        Assert.Equal(PayoutScreenStates.Paid, result!.ScreenState);
        Assert.Equal(claim.ClaimNumber, result.ClaimNumber);
        Assert.NotNull(result.LastPaidInstallment);
        Assert.Equal(record.InstallmentId, result.LastPaidInstallment!.InstallmentId);
        Assert.Equal("PLN", result.LastPaidInstallment.Currency);
        Assert.False(result.CanEdit);
    }

    [Fact]
    public async Task GetLastPaidInstallmentAsync_IncompleteRecord_ReturnsIncompleteData()
    {
        var (claim, claims) = await CreateClaimAsync();
        var record = new PayoutRecord(Guid.NewGuid(), 1, null, 100m, "PLN");
        var service = new PayoutService(claims, new FakePayoutRepository(record));

        var result = await service.GetLastPaidInstallmentAsync(claim.Id);

        Assert.NotNull(result);
        Assert.Equal(PayoutScreenStates.IncompleteData, result!.ScreenState);
        Assert.Null(result.LastPaidInstallment);
        Assert.Null(result.ClaimNumber);
    }
}
