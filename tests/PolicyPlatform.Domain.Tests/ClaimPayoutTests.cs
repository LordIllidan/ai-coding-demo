using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class ClaimPayoutTests
{
    private static ClaimPayout CreatePayout(
        Guid? claimId = null,
        Guid? customerId = null,
        string claimNumber = "CLM-2026-0001",
        decimal amountGross = 1234.5m,
        string currencyCode = "PLN",
        ClaimPayoutStatus status = ClaimPayoutStatus.Paid) => ClaimPayout.Register(
        Guid.NewGuid(),
        claimId ?? Guid.NewGuid(),
        claimNumber,
        customerId ?? Guid.NewGuid(),
        amountGross,
        currencyCode,
        new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
        status);

    [Fact]
    public void Register_EmptyClaimId_Throws()
    {
        Assert.Throws<DomainException>(() => CreatePayout(claimId: Guid.Empty));
    }

    [Fact]
    public void Register_EmptyCustomerId_Throws()
    {
        Assert.Throws<DomainException>(() => CreatePayout(customerId: Guid.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_MissingClaimNumber_Throws(string? claimNumber)
    {
        Assert.Throws<DomainException>(() => CreatePayout(claimNumber: claimNumber!));
    }

    [Fact]
    public void Register_NegativeAmount_Throws()
    {
        Assert.Throws<DomainException>(() => CreatePayout(amountGross: -0.01m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("PL")]
    [InlineData("PLNN")]
    public void Register_InvalidCurrencyCode_Throws(string currencyCode)
    {
        Assert.Throws<DomainException>(() => CreatePayout(currencyCode: currencyCode));
    }

    [Fact]
    public void Register_ValidData_SetsAllProperties()
    {
        var claimId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var payout = CreatePayout(claimId: claimId, customerId: customerId, status: ClaimPayoutStatus.Paid);

        Assert.Equal(claimId, payout.ClaimId);
        Assert.Equal(customerId, payout.CustomerId);
        Assert.Equal("CLM-2026-0001", payout.ClaimNumber);
        Assert.Equal(1234.5m, payout.AmountGross);
        Assert.Equal("PLN", payout.CurrencyCode);
        Assert.Equal(new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), payout.PaidAt);
        Assert.Equal(ClaimPayoutStatus.Paid, payout.Status);
    }

    [Fact]
    public void Register_TrimsClaimNumberAndUppercasesCurrency()
    {
        var payout = CreatePayout(claimNumber: "  CLM-2026-0002  ", currencyCode: "pln");

        Assert.Equal("CLM-2026-0002", payout.ClaimNumber);
        Assert.Equal("PLN", payout.CurrencyCode);
    }

    [Fact]
    public void Register_ZeroAmount_IsAllowed()
    {
        var payout = CreatePayout(amountGross: 0m);

        Assert.Equal(0m, payout.AmountGross);
    }
}
