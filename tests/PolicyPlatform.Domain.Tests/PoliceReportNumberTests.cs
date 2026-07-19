using PolicyPlatform.Domain.Claims;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class PoliceReportNumberTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryCreate_MissingValue_ReturnsRequiredError(string? value)
    {
        var ok = PoliceReportNumber.TryCreate(value, out _, out var error);

        Assert.False(ok);
        Assert.Equal(PoliceReportNumberError.Required, error);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("KMP#123")]
    public void TryCreate_InvalidFormat_ReturnsInvalidFormatError(string value)
    {
        var ok = PoliceReportNumber.TryCreate(value, out _, out var error);

        Assert.False(ok);
        Assert.Equal(PoliceReportNumberError.InvalidFormat, error);
    }

    [Fact]
    public void TryCreate_ValidValue_TrimsAndUppercases()
    {
        var ok = PoliceReportNumber.TryCreate("  kmp/123/2026  ", out var number, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("KMP/123/2026", number.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        PoliceReportNumber.TryCreate("KMP/123/2026", out var number, out _);

        Assert.Equal("KMP/123/2026", number.ToString());
    }

    [Fact]
    public void Create_ValidValue_TrimsAndUppercases()
    {
        var number = PoliceReportNumber.Create("  kmp/123/2026  ");

        Assert.Equal("KMP/123/2026", number.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("KMP#123")]
    public void Create_InvalidValue_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => PoliceReportNumber.Create(value));
    }
}
