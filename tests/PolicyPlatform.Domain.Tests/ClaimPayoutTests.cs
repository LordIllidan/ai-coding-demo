using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Policies;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class ClaimPayoutTests
{
    private static readonly Money DefaultAmount = new(1234.56m, "PLN");
    private static readonly DateTime DefaultPaidAt = new(2026, 6, 1);

    [Fact]
    public void Create_EmptyClaimId_Throws()
    {
        Assert.Throws<DomainException>(() => ClaimPayout.Create(
            Guid.NewGuid(), Guid.Empty, "SZK/1/2026", Guid.NewGuid(),
            DefaultAmount, DefaultPaidAt, ClaimPayoutStatus.Paid));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingClaimNumber_Throws(string? claimNumber)
    {
        Assert.Throws<DomainException>(() => ClaimPayout.Create(
            Guid.NewGuid(), Guid.NewGuid(), claimNumber!, Guid.NewGuid(),
            DefaultAmount, DefaultPaidAt, ClaimPayoutStatus.Paid));
    }

    [Fact]
    public void Create_EmptyCustomerId_Throws()
    {
        Assert.Throws<DomainException>(() => ClaimPayout.Create(
            Guid.NewGuid(), Guid.NewGuid(), "SZK/1/2026", Guid.Empty,
            DefaultAmount, DefaultPaidAt, ClaimPayoutStatus.Paid));
    }

    [Fact]
    public void Create_ValidArguments_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var payout = ClaimPayout.Create(
            id, claimId, "SZK/1/2026", customerId, DefaultAmount, DefaultPaidAt, ClaimPayoutStatus.Paid);

        Assert.Equal(id, payout.Id);
        Assert.Equal(claimId, payout.ClaimId);
        Assert.Equal("SZK/1/2026", payout.ClaimNumber);
        Assert.Equal(customerId, payout.CustomerId);
        Assert.Equal(DefaultAmount, payout.Amount);
        Assert.Equal(DefaultPaidAt, payout.PaidAt);
        Assert.Equal(ClaimPayoutStatus.Paid, payout.Status);
    }
}
