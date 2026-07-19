using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Application.Abstractions;

public interface IAssistanceReportRepository
{
    Task<AssistanceReport?> FindDuplicateAsync(
        Guid userId, Guid idempotencyKey, DateTime since, CancellationToken ct = default);

    /// <summary>Persists a newly registered report together with its creation event
    /// in a single commit, so the registration is never left half-written.</summary>
    Task RegisterAsync(AssistanceReport report, AssistanceReportEvent createdEvent, CancellationToken ct = default);

    /// <summary>Persists the outcome of a partner dispatch attempt (success or failure)
    /// for an already-registered report, together with its integration event.</summary>
    Task RecordDispatchOutcomeAsync(
        AssistanceReport report, AssistanceReportEvent outcomeEvent, CancellationToken ct = default);

    /// <summary>Appends a standalone integration event (e.g. PARTNER_DISPATCH_REQUESTED)
    /// without changing the report's persisted state.</summary>
    Task AddEventAsync(AssistanceReportEvent @event, CancellationToken ct = default);

    Task<IReadOnlyList<AssistanceReport>> GetDueForPartnerRetryAsync(
        DateTime now, int batchSize, CancellationToken ct = default);
}
