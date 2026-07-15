using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Policies;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class ValueObjectTests
{
    [Theory]
    [InlineData("POL-2026-000001")]
    [InlineData("POL-1999-123456")]
    public void PolicyNumber_ValidFormat_Accepted(string value)
    {
        var number = new PolicyNumber(value);
        Assert.Equal(value, number.Value);
    }

    [Theory]
    [InlineData("POL-26-000001")]
    [InlineData("pol-2026-000001")]
    [InlineData("POL-2026-1")]
    [InlineData("")]
    public void PolicyNumber_InvalidFormat_Throws(string value)
    {
        Assert.Throws<DomainException>(() => new PolicyNumber(value));
    }

    [Fact]
    public void Money_NegativeAmount_Throws()
    {
        Assert.Throws<DomainException>(() => new Money(-1m, "PLN"));
    }

    [Fact]
    public void Money_Add_DifferentCurrency_Throws()
    {
        var pln = new Money(100m, "PLN");
        var eur = new Money(50m, "EUR");

        Assert.Throws<DomainException>(() => pln.Add(eur));
    }

    [Fact]
    public void Money_Add_SameCurrency_SumsAmounts()
    {
        var a = new Money(100m, "PLN");
        var b = new Money(50m, "PLN");

        var result = a.Add(b);

        Assert.Equal(150m, result.Amount);
    }
}
