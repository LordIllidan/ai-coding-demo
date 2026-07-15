using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class PoliceReportNumberTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_MissingValue_Throws(string? value)
    {
        Assert.Throws<DomainException>(() => new PoliceReportNumber(value));
    }

    [Fact]
    public void Constructor_ValidValue_TrimsWhitespace()
    {
        var number = new PoliceReportNumber("  KMP/123/2026  ");

        Assert.Equal("KMP/123/2026", number.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var number = new PoliceReportNumber("KMP/123/2026");

        Assert.Equal("KMP/123/2026", number.ToString());
    }
}
