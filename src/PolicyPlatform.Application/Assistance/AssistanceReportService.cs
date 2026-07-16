using System.Globalization;
using System.Text.Json;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Application.Assistance;

/// <summary>Application service (use-case layer) for assistance report registration.
/// Registration is committed regardless of partner dispatch outcome — a partner failure
/// only flips partner_dispatch_status to FAILED_RETRY_SCHEDULED and schedules a retry;
/// it never blocks or rolls back the registration itself.</summary>
public sealed class AssistanceReportService
{
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromMinutes(1);

    private readonly IAssistanceReportRepository _reports;
    private readonly IPartnerAssistanceClient _partnerClient;

    public AssistanceReportService(IAssistanceReportRepository reports, IPartnerAssistanceClient partnerClient)
    {
        _reports = reports;
        _partnerClient = partnerClient;
    }

    public async Task<AssistanceReportDto> RegisterAsync(
        Guid userId, Guid idempotencyKey, CreateAssistanceReportRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

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
                throw new AssistanceDomainException(
                    AssistanceErrorCodes.InvalidCoordinates, "occurredAt must be a valid ISO-8601 date/time.");
            }

            occurredAt = parsed;
        }

        var existing = await _reports.FindDuplicateAsync(userId, idempotencyKey, now - IdempotencyWindow, ct);
        if (existing is not null)
        {
            if (!existing.MatchesSubmission(incidentType, gps))
            {
                throw new AssistanceDomainException(
                    AssistanceErrorCodes.DuplicateSubmission,
                    "This Idempotency-Key was already used for a different assistance report.");
            }

            return AssistanceReportDto.FromDomain(existing);
        }

        var report = AssistanceReport.Register(
            Guid.NewGuid(), userId, idempotencyKey, incidentType, gps, occurredAt, now);

        var createdEvent = AssistanceReportEvent.Create(
            report.Id, AssistanceEventType.ASSISTANCE_REPORT_CREATED,
            JsonSerializer.Serialize(new { report.Id, UserId = userId, IncidentType = incidentType.ToString() }),
            now);

        await _reports.RegisterAsync(report, createdEvent, ct);

        await DispatchToPartnerAsync(report, ct);

        return AssistanceReportDto.FromDomain(report);
    }

    /// <summary>Attempts a single partner dispatch for an already-registered report and
    /// persists the outcome. Never throws — any partner failure is recorded as a scheduled
    /// retry so it can never affect the (already committed) registration.</summary>
    public async Task DispatchToPartnerAsync(AssistanceReport report, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var requestedEvent = AssistanceReportEvent.Create(
            report.Id, AssistanceEventType.PARTNER_DISPATCH_REQUESTED, JsonSerializer.Serialize(new { }), now);
        await _reports.AddEventAsync(requestedEvent, ct);

        PartnerDispatchResult result;
        try
        {
            result = await _partnerClient.DispatchAsync(report, ct);
        }
        catch (Exception ex)
        {
            result = PartnerDispatchResult.Failed(ex.Message);
        }

        if (result.Success)
        {
            report.MarkPartnerDispatchSucceeded(result.PartnerCaseId!, now);
            var succeededEvent = AssistanceReportEvent.Create(
                report.Id, AssistanceEventType.PARTNER_DISPATCH_SUCCEEDED,
                JsonSerializer.Serialize(new { report.PartnerCaseId }), now);
            await _reports.RecordDispatchOutcomeAsync(report, succeededEvent, ct);
        }
        else
        {
            var nextAttemptAt = now + NextRetryDelay(report.PartnerDispatchAttempts);
            report.MarkPartnerDispatchFailed(nextAttemptAt, now);
            var failedEvent = AssistanceReportEvent.Create(
                report.Id, AssistanceEventType.PARTNER_DISPATCH_FAILED,
                JsonSerializer.Serialize(new { Reason = result.FailureReason, NextAttemptAt = nextAttemptAt }), now);
            await _reports.RecordDispatchOutcomeAsync(report, failedEvent, ct);
        }
    }

    private static TimeSpan NextRetryDelay(int attemptsSoFar)
    {
        var backoffMinutes = InitialRetryDelay.TotalMinutes * Math.Pow(2, attemptsSoFar);
        return TimeSpan.FromMinutes(Math.Min(backoffMinutes, TimeSpan.FromHours(1).TotalMinutes));
    }
}
