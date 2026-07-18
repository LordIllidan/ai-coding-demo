using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Claims;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class ClaimPayoutDtosTests
{
    private static ClaimPayout CreatePayout(decimal amountGross, string currencyCode, DateTime paidAt) =>
        ClaimPayout.Register(
            Guid.NewGuid(), Guid.NewGuid(), "CLM-2026-0001", Guid.NewGuid(),
            amountGross, currencyCode, paidAt, ClaimPayoutStatus.Paid);

    [Fact]
    public void FromDomain_MapsFieldsAndFormatsAmountToTwoDecimals()
    {
        var payout = CreatePayout(1234.5m, "PLN", new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc));

        var dto = LastPayoutDto.FromDomain(payout);

        Assert.Equal("CLM-2026-0001", dto.ClaimNumber);
        Assert.Equal("1234.50", dto.Amount.Value);
        Assert.Equal("PLN", dto.Amount.Currency);
        Assert.Equal("2026-03-10", dto.PayoutDate);
        Assert.True(dto.ReadOnly);
    }

    [Fact]
    public void FromDomain_RoundAmount_UsesInvariantCultureDecimalPoint()
    {
        var payout = CreatePayout(1000m, "PLN", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var dto = LastPayoutDto.FromDomain(payout);

        Assert.Equal("1000.00", dto.Amount.Value);
    }

    [Fact]
    public void FromDomain_TruncatesTimeComponentFromPaidAt()
    {
        var payout = CreatePayout(100m, "PLN", new DateTime(2026, 6, 15, 23, 45, 0, DateTimeKind.Utc));

        var dto = LastPayoutDto.FromDomain(payout);

        Assert.Equal("2026-06-15", dto.PayoutDate);
    }
}
