using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Policies;

/// <summary>Application service (use-case layer). Orchestrates domain objects and
/// repositories; contains no business rules itself — those live in the Domain.</summary>
public sealed class PolicyService
{
    private readonly IPolicyRepository _policies;
    private readonly ICustomerRepository _customers;
    private readonly IPolicyNumberGenerator _numberGenerator;

    public PolicyService(
        IPolicyRepository policies, ICustomerRepository customers, IPolicyNumberGenerator numberGenerator)
    {
        _policies = policies;
        _customers = customers;
        _numberGenerator = numberGenerator;
    }

    public async Task<PolicyDto> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, ct)
            ?? throw new DomainException($"Customer {request.CustomerId} was not found.");

        var number = await _numberGenerator.NextAsync(ct);
        var policy = Policy.CreateDraft(Guid.NewGuid(), number, customer.Id, request.EffectiveDate, request.ExpiryDate);

        foreach (var coverageRequest in request.Coverages)
        {
            policy.AddCoverage(new Coverage(
                coverageRequest.Type,
                new Money(coverageRequest.SumInsured, coverageRequest.Currency),
                new Money(coverageRequest.Premium, coverageRequest.Currency)));
        }

        await _policies.AddAsync(policy, ct);
        return PolicyDto.FromDomain(policy);
    }

    public async Task<PolicyDto> ActivatePolicyAsync(Guid policyId, CancellationToken ct = default)
    {
        var policy = await GetPolicyOrThrowAsync(policyId, ct);
        policy.Activate();
        await _policies.UpdateAsync(policy, ct);
        return PolicyDto.FromDomain(policy);
    }

    public async Task<PolicyDto> CancelPolicyAsync(Guid policyId, CancellationToken ct = default)
    {
        var policy = await GetPolicyOrThrowAsync(policyId, ct);
        policy.Cancel();
        await _policies.UpdateAsync(policy, ct);
        return PolicyDto.FromDomain(policy);
    }

    public async Task<PolicyDto?> GetPolicyAsync(Guid policyId, CancellationToken ct = default)
    {
        var policy = await _policies.GetByIdAsync(policyId, ct);
        return policy is null ? null : PolicyDto.FromDomain(policy);
    }

    public async Task<IReadOnlyList<PolicyDto>> ListPoliciesAsync(CancellationToken ct = default)
    {
        var policies = await _policies.ListAsync(ct);
        return policies.Select(PolicyDto.FromDomain).ToList();
    }

    private async Task<Policy> GetPolicyOrThrowAsync(Guid policyId, CancellationToken ct)
        => await _policies.GetByIdAsync(policyId, ct)
            ?? throw new DomainException($"Policy {policyId} was not found.");
}
