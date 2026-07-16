using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Application.Assistance;

public sealed record CreateAssistanceReportRequest(
    string? IncidentType,
    decimal? GpsLatitude,
    decimal? GpsLongitude,
    decimal? GpsAccuracyMeters,
    string? OccurredAt);

public sealed record AssistanceWarningDto(string Code, string Message);

public sealed record AssistanceReportDto(
    Guid ReportId,
    string Status,
    string IncidentType,
    string PartnerDispatchStatus,
    IReadOnlyList<AssistanceWarningDto>? Warnings,
    DateTime CreatedAt)
{
    public static AssistanceReportDto FromDomain(AssistanceReport report)
    {
        var warnings = report.PartnerDispatchStatus == PartnerDispatchStatus.FAILED_RETRY_SCHEDULED
            ? new[]
            {
                new AssistanceWarningDto(
                    "PARTNER_INTEGRATION_FAILED",
                    "Assistance partner dispatch failed; a retry has been scheduled."),
            }
            : null;

        return new AssistanceReportDto(
            report.Id,
            report.Status.ToString(),
            report.IncidentType.ToString(),
            report.PartnerDispatchStatus.ToString(),
            warnings,
            report.CreatedAt);
    }
}
