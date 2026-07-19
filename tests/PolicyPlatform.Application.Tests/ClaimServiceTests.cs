using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Policies;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class ClaimServiceTests
{
    private static (ClaimService Claims, InMemoryPolicyRepository Policies) CreateServices()
    {
        var policyRepo = new InMemoryPolicyRepository();
        var claimRepo = new InMemoryClaimRepository();
        return (new ClaimService(claimRepo, policyRepo), policyRepo);
    }

    private static async Task<Guid> CreateExistingPolicyAsync(InMemoryPolicyRepository policies)
    {
        var policy = Policy.CreateDraft(
            Guid.NewGuid(),
            new PolicyNumber("POL-2026-000001"),
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));
        await policies.AddAsync(policy);
        return policy.Id;
    }

    [Fact]
    public async Task RegisterTheftClaim_UnknownPolicy_Throws()
    {
        var (claims, _) = CreateServices();
        var request = new CreateTheftClaimRequest(Guid.NewGuid(), "KMP/123/2026");

        await Assert.ThrowsAsync<DomainException>(() => claims.RegisterTheftClaimAsync(request));
    }

    [Fact]
    public async Task RegisterTheftClaim_MissingPoliceReportNumber_ThrowsValidation()
    {
        var (claims, policies) = CreateServices();
        var policyId = await CreateExistingPolicyAsync(policies);
        var request = new CreateTheftClaimRequest(policyId, null);

        var ex = await Assert.ThrowsAsync<PoliceReportNumberValidationException>(
            () => claims.RegisterTheftClaimAsync(request));
        Assert.Equal(PoliceReportNumber.RequiredCode, ex.Code);
    }

    [Fact]
    public async Task RegisterTheftClaim_InvalidFormatPoliceReportNumber_ThrowsValidation()
    {
        var (claims, policies) = CreateServices();
        var policyId = await CreateExistingPolicyAsync(policies);
        var request = new CreateTheftClaimRequest(policyId, "AB");

        var ex = await Assert.ThrowsAsync<PoliceReportNumberValidationException>(
            () => claims.RegisterTheftClaimAsync(request));
        Assert.Equal(PoliceReportNumber.InvalidFormatCode, ex.Code);
    }

    [Fact]
    public async Task RegisterTheftClaim_ValidRequest_ReturnsPersistedClaimNormalizedToUpperCase()
    {
        var (claims, policies) = CreateServices();
        var policyId = await CreateExistingPolicyAsync(policies);
        var request = new CreateTheftClaimRequest(policyId, "kmp/123/2026");

        var claim = await claims.RegisterTheftClaimAsync(request);
        var fetched = await claims.GetTheftClaimAsync(claim.ClaimId);

        Assert.Equal(policyId, claim.PolicyId);
        Assert.Equal("KMP/123/2026", claim.PoliceReportNumber);
        Assert.Equal("ACCEPTED", claim.Status);
        Assert.True(claim.NextStepAllowed);
        Assert.NotNull(fetched);
        Assert.Equal(claim.ClaimId, fetched!.Id);
    }

    [Fact]
    public async Task GetTheftClaim_UnknownId_ReturnsNull()
    {
        var (claims, _) = CreateServices();

        var result = await claims.GetTheftClaimAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
