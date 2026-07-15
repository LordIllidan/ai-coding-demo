using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Policies;

public sealed record CoverageRequest(CoverageType Type, decimal SumInsured, decimal Premium, string Currency = "PLN");

public sealed record CreatePolicyRequest(
    Guid CustomerId,
    DateOnly EffectiveDate,
    DateOnly ExpiryDate,
    IReadOnlyList<CoverageRequest> Coverages);

public sealed record CoverageDto(string Type, decimal SumInsured, decimal Premium, string Currency);

public sealed record PolicyDto(
    Guid Id,
    string Number,
    Guid CustomerId,
    string Status,
    DateOnly EffectiveDate,
    DateOnly ExpiryDate,
    decimal TotalPremium,
    string Currency,
    IReadOnlyList<CoverageDto> Coverages)
{
    public static PolicyDto FromDomain(Policy policy) => new(
        policy.Id,
        policy.Number.Value,
        policy.CustomerId,
        policy.Status.ToString(),
        policy.EffectiveDate,
        policy.ExpiryDate,
        policy.TotalPremium.Amount,
        policy.TotalPremium.Currency,
        policy.Coverages.Select(c => new CoverageDto(
            c.Type.ToString(), c.SumInsured.Amount, c.Premium.Amount, c.Premium.Currency)).ToList());
}
