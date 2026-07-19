namespace PolicyPlatform.Application.Sms;

/// <summary>Use-case port behind POST /api/v1/sms/policy-status-requests (AISDLC-78). Given an
/// already input-validated request (well-formed policyNumber/pesel — see
/// <see cref="PolicyStatusRequestValidator"/>), decides the business outcome. Does not perform
/// input-shape validation or rate limiting — those are handled upstream by the controller — and
/// does not know about HTTP status codes.</summary>
public interface IPolicyStatusRequestHandler
{
    /// <summary>Resolves the business outcome for an already input-validated request.</summary>
    /// <param name="request">Normalized policy number and checksum-verified PESEL.</param>
    /// <param name="ct">Cancellation token for the downstream lookup.</param>
    /// <returns>The business-level reply (found, not-verified, rate-limited, or service-unavailable).</returns>
    Task<PolicyStatusReply> HandleAsync(PolicyStatusRequest request, CancellationToken ct = default);
}
