using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Customers;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class PeselTests
{
    [Theory]
    [InlineData("44051401359")]
    [InlineData("02070803628")]
    public void Constructor_ValidChecksum_Accepted(string value)
    {
        var pesel = new Pesel(value);

        Assert.Equal(value, pesel.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    [InlineData("4405140135A")]
    public void Constructor_InvalidFormat_Throws(string value)
    {
        Assert.Throws<DomainException>(() => new Pesel(value));
    }

    [Fact]
    public void Constructor_ValidFormatButBadChecksum_Throws()
    {
        Assert.Throws<DomainException>(() => new Pesel("44051401350"));
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var pesel = new Pesel("44051401359");

        Assert.Equal("44051401359", pesel.ToString());
    }
}
