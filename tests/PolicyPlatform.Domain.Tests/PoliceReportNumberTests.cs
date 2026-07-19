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
    [InlineData("/KMP123")]
    [InlineData("-KMP123")]
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
    public void Constructor_AllowsSpaceAndDashCharacters()
    {
        var number = new PoliceReportNumber("KMP 123-2026");

        Assert.Equal("KMP 123-2026", number.Value);
    }

    [Fact]
    public void Constructor_ExactlyThreeCharacters_IsValid()
    {
        var number = new PoliceReportNumber("AB1");

        Assert.Equal("AB1", number.Value);
    }

    [Fact]
    public void Constructor_ExactlyFiftyCharacters_IsValid()
    {
        var value = "A" + new string('1', 49);

        var number = new PoliceReportNumber(value);

        Assert.Equal(value, number.Value);
        Assert.Equal(50, number.Value.Length);
    }

    [Fact]
    public void Constructor_FiftyOneCharacters_ThrowsInvalidFormat()
    {
        var value = "A" + new string('1', 50);

        var ex = Assert.Throws<PoliceReportNumberValidationException>(() => new PoliceReportNumber(value));
        Assert.Equal(PoliceReportNumber.InvalidFormatCode, ex.Code);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var number = new PoliceReportNumber("KMP/123/2026");

        Assert.Equal("KMP/123/2026", number.ToString());
    }
}
