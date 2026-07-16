using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Claims;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class ClaimDtosTests
{
    private static readonly DateTime RegisteredAt = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    private static TheftClaim CreateClaim(Guid id, Guid policyId) => TheftClaim.Register(
        id, policyId, new PoliceReportNumber("KMP/123/2026"), RegisteredAt);

    [Fact]
    public void TheftClaimCreatedResponse_FromDomain_MapsAllFieldsAndSetsNextStepAllowedTrue()
    {
        var id = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var claim = CreateClaim(id, policyId);

        var response = TheftClaimCreatedResponse.FromDomain(claim);

        Assert.Equal(id, response.ClaimId);
        Assert.Equal(policyId, response.PolicyId);
        Assert.Equal("KMP/123/2026", response.PoliceReportNumber);
        Assert.Equal(TheftClaim.StatusAccepted, response.Status);
        Assert.True(response.NextStepAllowed);
    }

    [Fact]
    public void TheftClaimDto_FromDomain_MapsAllFields()
    {
        var id = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var claim = CreateClaim(id, policyId);

        var dto = TheftClaimDto.FromDomain(claim);

        Assert.Equal(id, dto.Id);
        Assert.Equal(policyId, dto.PolicyId);
        Assert.Equal("KMP/123/2026", dto.PoliceReportNumber);
        Assert.Equal(TheftClaim.StatusAccepted, dto.Status);
        Assert.Equal(RegisteredAt, dto.CreatedAt);
        Assert.Equal(RegisteredAt, dto.UpdatedAt);
    }
}
