using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Customers;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Sms;

/// <summary>Use case behind POST /api/v1/sms/policy-status-requests (AISDLC-78). Decides the
/// business outcome of a policy-status lookup; does not perform input-shape validation or rate
/// limiting (handled upstream) and does not know about HTTP status codes.</summary>
public sealed class PolicyStatusRequestService
{
    private readonly IPolicyStatusLookupRepository _lookup;

    public PolicyStatusRequestService(IPolicyStatusLookupRepository lookup) => _lookup = lookup;

    public async Task<PolicyStatusReply> HandleAsync(PolicyStatusRequest request, CancellationToken ct = default)
    {
        var requestId = Guid.NewGuid();

        PolicyNumber policyNumber;
        Pesel pesel;
        try
        {
            policyNumber = new PolicyNumber(request.PolicyNumber);
            pesel = new Pesel(request.Pesel);
        }
        catch (DomainException)
        {
            // Well-formed-per-upstream-validation but not a real policy number/PESEL shape:
            // still falls under "no disclosure whether a policy exists".
            return PolicyStatusReplyMapper.NotVerified(requestId);
        }

        PolicyStatus? status;
        try
        {
            status = await _lookup.FindPolicyStatusAsync(policyNumber, pesel, ct);
        }
        catch (PolicyStatusLookupUnavailableException)
        {
            return PolicyStatusReplyMapper.ServiceUnavailable(requestId);
        }

        return status is null
            ? PolicyStatusReplyMapper.NotVerified(requestId)
            : PolicyStatusReplyMapper.Found(requestId, status.Value);
    }
}
