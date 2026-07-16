using PolicyPlatform.Domain.Customers;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Abstractions;

/// <summary>Port used by the SMS policy-status use case. The single lookup method must return
/// null both when the policy number does not exist and when it exists but the PESEL does not
/// match its holder — the two cases are indistinguishable by design so implementations cannot
/// accidentally leak whether a policy exists via a richer return type.</summary>
public interface IPolicyStatusLookupRepository
{
    /// <summary>Looks up a policy's disclosable status for the given holder.</summary>
    /// <param name="policyNumber">Policy number as submitted by the SMS sender.</param>
    /// <param name="pesel">PESEL expected to match the policy holder.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The policy status if found and the PESEL matches the holder; otherwise
    /// <see langword="null"/> — including when the policy number does not exist at all.</returns>
    /// <exception cref="PolicyStatusLookupUnavailableException">The lookup could not be completed
    /// (e.g. a downstream dependency failure).</exception>
    Task<PolicyStatus?> FindPolicyStatusAsync(PolicyNumber policyNumber, Pesel pesel, CancellationToken ct = default);
}
