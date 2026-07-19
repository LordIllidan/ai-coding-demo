using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Customers;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class PeselTests
{
    [Theory]
    [InlineData("44051401359")]
    [InlineData("02070803628")]
    public void Pesel_ValidChecksum_Accepted(string value)
    {
        var pesel = new Pesel(value);
        Assert.Equal(value, pesel.Value);
    }

    [Theory]
    [InlineData("4405140135")]
    [InlineData("440514013599")]
    [InlineData("4405140135a")]
    [InlineData("")]
    public void Pesel_WrongLengthOrNonDigits_Throws(string value)
    {
        Assert.Throws<DomainException>(() => new Pesel(value));
    }

    [Fact]
    public void Pesel_InvalidChecksum_Throws()
    {
        Assert.Throws<DomainException>(() => new Pesel("44051401350"));
    }

    [Fact]
    public void Pesel_ToString_ReturnsValue()
    {
        var pesel = new Pesel("44051401359");
        Assert.Equal("44051401359", pesel.ToString());
    }
}
