using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Application.Abstractions;

public sealed record PartnerDispatchResult(bool Success, string? PartnerCaseId, string? FailureReason)
{
    public static PartnerDispatchResult Succeeded(string partnerCaseId) => new(true, partnerCaseId, null);
    public static PartnerDispatchResult Failed(string reason) => new(false, null, reason);
}

/// <summary>Outbound integration with the assistance partner dispatch system.
/// Implementations must never throw for ordinary integration failures — they should
/// return a failed <see cref="PartnerDispatchResult"/> so the caller can schedule a retry
/// without ever blocking or rolling back the report registration itself.</summary>
public interface IPartnerAssistanceClient
{
    Task<PartnerDispatchResult> DispatchAsync(AssistanceReport report, CancellationToken ct = default);
}
