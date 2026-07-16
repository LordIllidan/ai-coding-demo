using PolicyPlatform.Domain.Claims;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class PoliceReportNumberTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_MissingValue_ThrowsRequired(string? value)
    {
        var ex = Assert.Throws<PoliceReportNumberValidationException>(() => new PoliceReportNumber(value));
        Assert.Equal(PoliceReportNumber.RequiredCode, ex.Code);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("KMP@123")]
    [InlineData("KMP_123/2026")]
    public void Constructor_InvalidFormat_ThrowsInvalidFormat(string value)
    {
        var ex = Assert.Throws<PoliceReportNumberValidationException>(() => new PoliceReportNumber(value));
        Assert.Equal(PoliceReportNumber.InvalidFormatCode, ex.Code);
    }

    [Fact]
    public void Constructor_ValidValue_TrimsAndNormalizesToUpperCase()
    {
        var number = new PoliceReportNumber("  kmp/123/2026  ");

        Assert.Equal("KMP/123/2026", number.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var number = new PoliceReportNumber("KMP/123/2026");

        Assert.Equal("KMP/123/2026", number.ToString());
    }
}
