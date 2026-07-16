using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Application.Assistance;

/// <summary>Result of validating a <see cref="CreateAssistanceReportRequest"/>: parsed,
/// range-checked fields ready for registration by the (separately owned) persistence layer.</summary>
public sealed record ValidatedAssistanceReportRequest(
    IncidentType IncidentType,
    GpsLocation Gps,
    DateTime? OccurredAt);
