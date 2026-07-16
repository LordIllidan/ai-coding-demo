using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Assistance;

public sealed class AssistanceReport : Entity
{
    public Guid UserId { get; }
    public Guid IdempotencyKey { get; }
    public IncidentType IncidentType { get; }
    public GpsLocation Gps { get; }
    public DateTime? OccurredAt { get; }
    public AssistanceReportStatus Status { get; private set; }
    public PartnerDispatchStatus PartnerDispatchStatus { get; private set; }
    public string? PartnerCaseId { get; private set; }
    public int PartnerDispatchAttempts { get; private set; }
    public DateTime? NextPartnerDispatchAttemptAt { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private AssistanceReport(
        Guid id, Guid userId, Guid idempotencyKey, IncidentType incidentType, GpsLocation gps,
        DateTime? occurredAt, DateTime createdAt)
        : base(id)
    {
        UserId = userId;
        IdempotencyKey = idempotencyKey;
        IncidentType = incidentType;
        Gps = gps;
        OccurredAt = occurredAt;
        Status = AssistanceReportStatus.REGISTERED;
        PartnerDispatchStatus = PartnerDispatchStatus.PENDING;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static AssistanceReport Register(
        Guid id, Guid userId, Guid idempotencyKey, IncidentType incidentType, GpsLocation gps,
        DateTime? occurredAt, DateTime now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Assistance report must reference an authenticated user.");
        }

        if (occurredAt is { } value && value > now.AddMinutes(5))
        {
            throw new AssistanceDomainException(
                AssistanceErrorCodes.InvalidCoordinates,
                "occurredAt cannot be more than 5 minutes in the future.");
        }

        return new AssistanceReport(id, userId, idempotencyKey, incidentType, gps, occurredAt, now);
    }

    /// <summary>Matches the fields that make two submissions under the same
    /// idempotency key "the same request" for idempotent-replay purposes.</summary>
    public bool MatchesSubmission(IncidentType incidentType, GpsLocation gps)
        => IncidentType == incidentType && Gps.Latitude == gps.Latitude && Gps.Longitude == gps.Longitude;

    public void MarkPartnerDispatchSucceeded(string partnerCaseId, DateTime now)
    {
        PartnerDispatchStatus = PartnerDispatchStatus.SENT;
        PartnerCaseId = partnerCaseId;
        NextPartnerDispatchAttemptAt = null;
        UpdatedAt = now;
    }

    public void MarkPartnerDispatchFailed(DateTime nextAttemptAt, DateTime now)
    {
        PartnerDispatchStatus = PartnerDispatchStatus.FAILED_RETRY_SCHEDULED;
        PartnerDispatchAttempts += 1;
        NextPartnerDispatchAttemptAt = nextAttemptAt;
        UpdatedAt = now;
    }
}
