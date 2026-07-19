using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Assistance;

/// <summary>Reference-type record (not a record struct): EF Core's OwnsOne requires a
/// reference type for nested owned value objects. See Policies.Money for the same rule.</summary>
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
