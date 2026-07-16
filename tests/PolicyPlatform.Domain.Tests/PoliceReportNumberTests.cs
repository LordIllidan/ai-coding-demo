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
    public void Constructor_MissingValue_ThrowsFieldValidationExceptionWithRequiredCode(string? value)
    {
        var ex = Assert.Throws<FieldValidationException>(() => new PoliceReportNumber(value));

        Assert.Equal("policeReportNumber", ex.Field);
        Assert.Equal("POLICE_REPORT_NUMBER_REQUIRED", ex.Code);
    }

    [Fact]
    public void Constructor_ValidValue_TrimsWhitespace()
    {
        var number = new PoliceReportNumber("  KMP/123/2026  ");

        Assert.Equal("KMP/123/2026", number.Value);
    }

    [Fact]
    public void Constructor_LowercaseValue_NormalizesToUppercase()
    {
        var number = new PoliceReportNumber("kmp/123/2026");

        Assert.Equal("KMP/123/2026", number.Value);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("A!")]
    [InlineData("KMP#123")]
    [InlineData("-ABC123")]
    public void Constructor_InvalidFormat_ThrowsFieldValidationExceptionWithFormatCode(string value)
    {
        var ex = Assert.Throws<FieldValidationException>(() => new PoliceReportNumber(value));

        Assert.Equal("policeReportNumber", ex.Field);
        Assert.Equal("POLICE_REPORT_NUMBER_INVALID_FORMAT", ex.Code);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("KMP/123/2026")]
    [InlineData("KMP 123-2026")]
    public void Constructor_ValidFormat_DoesNotThrow(string value)
    {
        var number = new PoliceReportNumber(value);

        Assert.Equal(value.ToUpperInvariant(), number.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var number = new PoliceReportNumber("KMP/123/2026");

        Assert.Equal("KMP/123/2026", number.ToString());
    }
}
