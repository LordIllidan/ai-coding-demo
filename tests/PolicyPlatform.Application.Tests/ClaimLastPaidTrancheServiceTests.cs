using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class ClaimLastPaidTrancheServiceTests
{
    private sealed class AllowingAccessValidator : IClaimAccessValidator
    {
        public Task EnsureAccessAsync(string? authorizationHeaderValue, Guid claimId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class ThrowingTrancheClient(Exception exception) : ITrancheIntegrationClient
    {
        public Task<LastPaidTrancheDto?> GetLastPaidTrancheAsync(Guid claimId, CancellationToken ct = default)
            => throw exception;
    }

    private sealed class StubTrancheClient(LastPaidTrancheDto? result) : ITrancheIntegrationClient
    {
        public Task<LastPaidTrancheDto?> GetLastPaidTrancheAsync(Guid claimId, CancellationToken ct = default)
            => Task.FromResult(result);
    }

    private static async Task<Guid> SeedClaimAsync(InMemoryClaimRepository claims)
    {
        var claim = TheftClaim.Register(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), "Kradziez.",
            new PoliceReportNumber("KMP/1/2026"), DateTime.UtcNow);
        await claims.AddAsync(claim);
        return claim.Id;
    }

    [Fact]
    public async Task GetLastPaidTranche_UnknownClaim_ThrowsClaimNotFound()
    {
        var service = new ClaimLastPaidTrancheService(
            new InMemoryClaimRepository(), new AllowingAccessValidator(),
            new StubTrancheClient(null), new InMemoryClaimLastPaidTrancheViewRepository());

        await Assert.ThrowsAsync<ClaimNotFoundException>(
            () => service.GetLastPaidTrancheAsync(Guid.NewGuid(), "Bearer token"));
    }

    [Fact]
    public async Task GetLastPaidTranche_NoTrancheYet_ReturnsNullTrancheNotAnError()
    {
        var claims = new InMemoryClaimRepository();
        var claimId = await SeedClaimAsync(claims);
        var service = new ClaimLastPaidTrancheService(
            claims, new AllowingAccessValidator(), new StubTrancheClient(null),
            new InMemoryClaimLastPaidTrancheViewRepository());

        var result = await service.GetLastPaidTrancheAsync(claimId, "Bearer token");

        Assert.Equal(claimId, result.ClaimId);
        Assert.Null(result.LastPaidTranche);
    }

    [Fact]
    public async Task GetLastPaidTranche_DownstreamTimesOut_PropagatesWithoutFallingBackToView()
    {
        var claims = new InMemoryClaimRepository();
        var claimId = await SeedClaimAsync(claims);
        var view = new InMemoryClaimLastPaidTrancheViewRepository();
        await view.UpsertAsync(new ClaimLastPaidTrancheViewRecord(
            claimId, Guid.NewGuid(), 1, "PAID", DateTimeOffset.UtcNow, 100m, "PLN",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var service = new ClaimLastPaidTrancheService(
            claims, new AllowingAccessValidator(),
            new ThrowingTrancheClient(new TrancheServiceTimeoutException()), view);

        await Assert.ThrowsAsync<TrancheServiceTimeoutException>(
            () => service.GetLastPaidTrancheAsync(claimId, "Bearer token"));
    }

    [Fact]
    public async Task GetLastPaidTranche_MissingAuthorizationHeader_ThrowsInvalidToken()
    {
        var validator = new BearerRejectsMissingHeaderValidator();
        var claims = new InMemoryClaimRepository();
        var claimId = await SeedClaimAsync(claims);
        var service = new ClaimLastPaidTrancheService(
            claims, validator, new StubTrancheClient(null), new InMemoryClaimLastPaidTrancheViewRepository());

        await Assert.ThrowsAsync<InvalidTokenException>(
            () => service.GetLastPaidTrancheAsync(claimId, authorizationHeaderValue: null));
    }

    private sealed class BearerRejectsMissingHeaderValidator : IClaimAccessValidator
    {
        public Task EnsureAccessAsync(string? authorizationHeaderValue, Guid claimId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeaderValue))
            {
                throw new InvalidTokenException();
            }

            return Task.CompletedTask;
        }
    }
}
