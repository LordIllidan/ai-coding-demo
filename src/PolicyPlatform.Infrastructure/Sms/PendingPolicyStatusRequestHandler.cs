using PolicyPlatform.Application.Sms;

namespace PolicyPlatform.Infrastructure.Sms;

/// <summary>Placeholder for the decision logic behind POST /api/v1/sms/policy-status-requests
/// (AISDLC-86 — policy/PESEL lookup and REPLIED/NOT_VERIFIED mapping — is a separate, unscoped
/// piece of work). Always reports SERVICE_UNAVAILABLE, which is an honest answer: no downstream
/// lookup is wired in yet, and this never leaks whether a policy exists.</summary>
public sealed class PendingPolicyStatusRequestHandler : IPolicyStatusRequestHandler
{
    /// <summary>Always returns SERVICE_UNAVAILABLE; no lookup is performed.</summary>
    /// <param name="request">Ignored — no downstream lookup is wired in yet.</param>
    /// <param name="ct">Unused.</param>
    /// <returns>A SERVICE_UNAVAILABLE reply with a freshly generated request id.</returns>
    public Task<PolicyStatusReply> HandleAsync(PolicyStatusRequest request, CancellationToken ct = default)
        => Task.FromResult(PolicyStatusReplyMapper.ServiceUnavailable(Guid.NewGuid()));
}
