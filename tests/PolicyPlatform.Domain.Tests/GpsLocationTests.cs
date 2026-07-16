using PolicyPlatform.Domain.Assistance;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class GpsLocationTests
{
    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Constructor_OutOfRangeCoordinates_ThrowsInvalidCoordinates(decimal lat, decimal lon)
    {
        var ex = Assert.Throws<AssistanceDomainException>(() => new GpsLocation(lat, lon, null));
        Assert.Equal(AssistanceErrorCodes.InvalidCoordinates, ex.Code);
    }

    [Fact]
    public void Constructor_NonPositiveAccuracy_ThrowsInvalidCoordinates()
    {
        var ex = Assert.Throws<AssistanceDomainException>(() => new GpsLocation(52.2m, 21.0m, 0m));
        Assert.Equal(AssistanceErrorCodes.InvalidCoordinates, ex.Code);
    }

    [Fact]
    public void Constructor_ValidCoordinates_SetsProperties()
    {
        var gps = new GpsLocation(52.2297m, 21.0122m, 5.5m);

        Assert.Equal(52.2297m, gps.Latitude);
        Assert.Equal(21.0122m, gps.Longitude);
        Assert.Equal(5.5m, gps.AccuracyMeters);
    }
}
