namespace PolicyPlatform.Application.Assistance;

/// <summary>Raw request body for POST /api/v1/assistance/reports, before validation.
/// customerId/policyId/userId are deliberately absent — the user and vehicle are
/// identified from the caller's JWT, per the assistance-reports contract.</summary>
public sealed record CreateAssistanceReportRequest(
    string? IncidentType,
    decimal? GpsLatitude,
    decimal? GpsLongitude,
    decimal? GpsAccuracyMeters,
    string? OccurredAt);
