using System.Globalization;
using PolicyPlatform.Domain.Assistance;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Application.Assistance;

/// <summary>Validates a POST /api/v1/assistance/reports request against the contract rules:
/// incident type, GPS coordinates, and the "occurredAt must not be more than 5 minutes in
/// the future" rule. Stateless — idempotency-key duplicate detection is a persistence
/// concern owned elsewhere and is out of scope here.</summary>
public static class AssistanceReportRequestValidator
{
    private static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(5);

    public static ValidatedAssistanceReportRequest Validate(CreateAssistanceReportRequest request, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(request.IncidentType) ||
            !Enum.TryParse<IncidentType>(request.IncidentType, out var incidentType))
        {
            throw new AssistanceDomainException(
                AssistanceErrorCodes.InvalidIncidentType,
                $"'{request.IncidentType}' is not a supported assistance incident type.");
        }

        if (request.GpsLatitude is null || request.GpsLongitude is null)
        {
            throw new AssistanceDomainException(
                AssistanceErrorCodes.GpsLocationRequired,
                "GPS location (gpsLatitude, gpsLongitude) is required.");
        }

        var gps = new GpsLocation(request.GpsLatitude.Value, request.GpsLongitude.Value, request.GpsAccuracyMeters);

        DateTime? occurredAt = null;
        if (!string.IsNullOrWhiteSpace(request.OccurredAt))
        {
            if (!DateTime.TryParse(
                    request.OccurredAt, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
            {
                throw new DomainException("occurredAt must be a valid ISO-8601 date/time.");
            }

            if (parsed > utcNow + MaxFutureSkew)
            {
                throw new DomainException("occurredAt cannot be more than 5 minutes in the future.");
            }

            occurredAt = parsed;
        }

        return new ValidatedAssistanceReportRequest(incidentType, gps, occurredAt);
    }
}
