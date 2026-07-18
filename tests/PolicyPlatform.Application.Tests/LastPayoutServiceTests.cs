using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class LastPayoutServiceTests
{
    private sealed class FakeIdentityResolver : ICustomerIdentityResolver
    {
        private readonly Func<string?, Guid> _resolve;

        public FakeIdentityResolver(Func<string?, Guid> resolve) => _resolve = resolve;

        public string? ReceivedHeader { get; private set; }

        public Guid ResolveCustomerId(string? authorizationHeaderValue)
        {
            ReceivedHeader = authorizationHeaderValue;
            return _resolve(authorizationHeaderValue);
        }
    }

    private sealed class FakeLastPayoutRepository : ILastPayoutRepository
    {
        private readonly LastPayoutRecord? _record;

        public FakeLastPayoutRepository(LastPayoutRecord? record) => _record = record;

        public Guid? ReceivedCustomerId { get; private set; }

        public Task<LastPayoutRecord?> GetLastPayoutAsync(Guid customerId, CancellationToken ct = default)
        {
            ReceivedCustomerId = customerId;
            return Task.FromResult(_record);
        }
    }

    [Fact]
    public async Task GetLastPayoutAsync_ResolvesCustomerFromHeader_AndQueriesRepositoryWithResolvedId()
    {
        var customerId = Guid.NewGuid();
        var identity = new FakeIdentityResolver(_ => customerId);
        var record = new LastPayoutRecord("PL-1001", 1500.00m, "PLN", new DateOnly(2026, 3, 10));
        var repository = new FakeLastPayoutRepository(record);
        var service = new LastPayoutService(identity, repository);

        var response = await service.GetLastPayoutAsync("Bearer abc.def.ghi");

        Assert.Equal("Bearer abc.def.ghi", identity.ReceivedHeader);
        Assert.Equal(customerId, repository.ReceivedCustomerId);
        Assert.Equal("PL-1001", response.ClaimNumber);
        Assert.True(response.ReadOnly);
    }

    [Fact]
    public async Task GetLastPayoutAsync_RepositoryReturnsNull_ThrowsLastPayoutNotFoundWithResolvedCustomerId()
    {
        var customerId = Guid.NewGuid();
        var identity = new FakeIdentityResolver(_ => customerId);
        var repository = new FakeLastPayoutRepository(null);
        var service = new LastPayoutService(identity, repository);

        var exception = await Assert.ThrowsAsync<LastPayoutNotFoundException>(
            () => service.GetLastPayoutAsync("Bearer abc.def.ghi"));

        Assert.Equal(customerId, exception.CustomerId);
    }

    [Fact]
    public async Task GetLastPayoutAsync_IdentityResolverThrowsAuthRequired_PropagatesWithoutCallingRepository()
    {
        var identity = new FakeIdentityResolver(_ => throw new AuthRequiredException());
        var repository = new FakeLastPayoutRepository(new LastPayoutRecord("PL-1", 1m, "PLN", new DateOnly(2026, 1, 1)));
        var service = new LastPayoutService(identity, repository);

        await Assert.ThrowsAsync<AuthRequiredException>(() => service.GetLastPayoutAsync(null));

        Assert.Null(repository.ReceivedCustomerId);
    }

    [Fact]
    public async Task GetLastPayoutAsync_IdentityResolverThrowsForbidden_Propagates()
    {
        var identity = new FakeIdentityResolver(_ => throw new ForbiddenCrossCustomerException());
        var repository = new FakeLastPayoutRepository(null);
        var service = new LastPayoutService(identity, repository);

        await Assert.ThrowsAsync<ForbiddenCrossCustomerException>(
            () => service.GetLastPayoutAsync("Bearer some.token.value"));
    }

    [Fact]
    public void LastPayoutResponse_FromRecord_MapsAmountAsTwoDecimalInvariantString()
    {
        var record = new LastPayoutRecord("PL-2002", 3250.5m, "PLN", new DateOnly(2026, 3, 10));

        var response = LastPayoutResponse.FromRecord(record);

        Assert.Equal("PL-2002", response.ClaimNumber);
        Assert.Equal("3250.50", response.Amount.Value);
        Assert.Equal("PLN", response.Amount.Currency);
        Assert.Equal("2026-03-10", response.PayoutDate);
        Assert.True(response.ReadOnly);
    }

    [Fact]
    public void LastPayoutResponse_FromRecord_RoundsAmountToTwoDecimals()
    {
        var record = new LastPayoutRecord("PL-3003", 100m, "PLN", new DateOnly(2026, 1, 5));

        var response = LastPayoutResponse.FromRecord(record);

        Assert.Equal("100.00", response.Amount.Value);
    }
}
