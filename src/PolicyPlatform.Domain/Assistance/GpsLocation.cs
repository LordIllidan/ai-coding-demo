namespace PolicyPlatform.Domain.Assistance;

/// <summary>GPS location value object. Validates the coordinate ranges and optional
/// accuracy from the assistance-reports contract (ASSISTANCE_003 INVALID_COORDINATES).</summary>
public sealed record GpsLocation
{
    public decimal Latitude { get; }
    public decimal Longitude { get; }
    public decimal? AccuracyMeters { get; }

    public GpsLocation(decimal latitude, decimal longitude, decimal? accuracyMeters)
    {
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            throw new AssistanceDomainException(
                AssistanceErrorCodes.InvalidCoordinates,
                "GPS coordinates are out of range (latitude -90..90, longitude -180..180).");
        }

        if (accuracyMeters is <= 0)
        {
            throw new AssistanceDomainException(
                AssistanceErrorCodes.InvalidCoordinates,
                "GPS accuracy, when provided, must be greater than zero.");
        }

        Latitude = latitude;
        Longitude = longitude;
        AccuracyMeters = accuracyMeters;
    }
}
