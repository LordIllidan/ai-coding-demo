using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Assistance;

public sealed class AssistanceReportEvent : Entity
{
    public Guid ReportId { get; }
    public AssistanceEventType EventType { get; }
    public string Payload { get; }
    public DateTime CreatedAt { get; }

    private AssistanceReportEvent(
        Guid id, Guid reportId, AssistanceEventType eventType, string payload, DateTime createdAt)
        : base(id)
    {
        ReportId = reportId;
        EventType = eventType;
        Payload = payload;
        CreatedAt = createdAt;
    }

    public static AssistanceReportEvent Create(
        Guid reportId, AssistanceEventType eventType, string payload, DateTime createdAt)
        => new(Guid.NewGuid(), reportId, eventType, payload, createdAt);
}
