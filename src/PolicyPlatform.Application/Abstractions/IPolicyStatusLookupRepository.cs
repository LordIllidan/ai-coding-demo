using PolicyPlatform.Domain.Customers;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Abstractions;

/// <summary>Port used by the SMS policy-status use case. The single lookup method must return
/// null both when the policy number does not exist and when it exists but the PESEL does not
/// match its holder — the two cases are indistinguishable by design so implementations cannot
/// accidentally leak whether a policy exists via a richer return type.</summary>
public interface IPolicyStatusLookupRepository
{
    Task<PolicyStatus?> FindPolicyStatusAsync(PolicyNumber policyNumber, Pesel pesel, CancellationToken ct = default);
}
