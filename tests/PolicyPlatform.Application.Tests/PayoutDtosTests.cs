using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Claims;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class PayoutDtosTests
{
    private static TheftClaim CreateClaim() => TheftClaim.Register(
        Guid.NewGuid(),
        Guid.NewGuid(),
        new DateOnly(2026, 1, 1),
        "Kradziez pojazdu.",
        new PoliceReportNumber("KMP/123/2026"),
        new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

    private static PayoutRecord CompleteRecord(
        Guid? installmentId = null, int? installmentNo = 1, DateOnly? paidAt = null,
        decimal? amount = 100m, string? currency = "PLN")
        => new(
            installmentId ?? Guid.NewGuid(),
            installmentNo,
            paidAt ?? new DateOnly(2026, 3, 1),
            amount,
            currency);

    [Fact]
    public void From_NullRecord_ReturnsNoPayout()
    {
        var claim = CreateClaim();

        var response = ClaimLastPaidInstallmentResponse.From(claim, null);

        Assert.Equal(claim.Id, response.ClaimId);
        Assert.Equal(PayoutScreenStates.NoPayout, response.ScreenState);
        Assert.Null(response.LastPaidInstallment);
        Assert.False(response.CanEdit);
        Assert.Null(response.ClaimNumber);
    }

    [Fact]
    public void From_CompleteRecord_ReturnsPaidWithUppercasedTrimmedCurrency()
    {
        var claim = CreateClaim();
        var record = CompleteRecord(currency: " pln ");

        var response = ClaimLastPaidInstallmentResponse.From(claim, record);

        Assert.Equal(PayoutScreenStates.Paid, response.ScreenState);
        Assert.Equal(claim.ClaimNumber, response.ClaimNumber);
        Assert.False(response.CanEdit);
        Assert.NotNull(response.LastPaidInstallment);
        Assert.Equal("PLN", response.LastPaidInstallment!.Currency);
    }

    [Fact]
    public void From_InstallmentNoZero_ReturnsIncompleteData()
    {
        var claim = CreateClaim();
        var record = CompleteRecord(installmentNo: 0);

        var response = ClaimLastPaidInstallmentResponse.From(claim, record);

        Assert.Equal(PayoutScreenStates.IncompleteData, response.ScreenState);
        Assert.Null(response.LastPaidInstallment);
        Assert.Null(response.ClaimNumber);
    }

    [Fact]
    public void From_InstallmentNoNull_ReturnsIncompleteData()
    {
        var claim = CreateClaim();
        var record = CompleteRecord(installmentNo: null);

        var response = ClaimLastPaidInstallmentResponse.From(claim, record);

        Assert.Equal(PayoutScreenStates.IncompleteData, response.ScreenState);
    }

    [Fact]
    public void From_MissingInstallmentId_ReturnsIncompleteData()
    {
        var claim = CreateClaim();
        var record = CompleteRecord() with { InstallmentId = null };

        var response = ClaimLastPaidInstallmentResponse.From(claim, record);

        Assert.Equal(PayoutScreenStates.IncompleteData, response.ScreenState);
    }

    [Fact]
    public void From_MissingPaidAt_ReturnsIncompleteData()
    {
        var claim = CreateClaim();
        var record = CompleteRecord() with { PaidAt = null };

        var response = ClaimLastPaidInstallmentResponse.From(claim, record);

        Assert.Equal(PayoutScreenStates.IncompleteData, response.ScreenState);
    }

    [Fact]
    public void From_MissingAmount_ReturnsIncompleteData()
    {
        var claim = CreateClaim();
        var record = CompleteRecord() with { Amount = null };

        var response = ClaimLastPaidInstallmentResponse.From(claim, record);

        Assert.Equal(PayoutScreenStates.IncompleteData, response.ScreenState);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_MissingOrBlankCurrency_ReturnsIncompleteData(string? currency)
    {
        var claim = CreateClaim();
        var record = CompleteRecord(currency: currency);

        var response = ClaimLastPaidInstallmentResponse.From(claim, record);

        Assert.Equal(PayoutScreenStates.IncompleteData, response.ScreenState);
    }
}
