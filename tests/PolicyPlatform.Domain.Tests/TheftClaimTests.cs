using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class TheftClaimTests
{
    private static readonly DateTime RegisteredAt = new(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

    private static TheftClaim CreateClaim(Guid policyId) => TheftClaim.Register(
        Guid.NewGuid(),
        policyId,
        new PoliceReportNumber("KMP/123/2026"),
        RegisteredAt);

    [Fact]
    public void Register_EmptyPolicyId_Throws()
    {
        Assert.Throws<DomainException>(() => CreateClaim(Guid.Empty));
    }

    [Fact]
    public void Register_ValidData_SetsAllProperties()
    {
        var policyId = Guid.NewGuid();

        var claim = CreateClaim(policyId);

        Assert.Equal(policyId, claim.PolicyId);
        Assert.Equal("KMP/123/2026", claim.PoliceReportNumber.Value);
        Assert.Equal(TheftClaim.StatusAccepted, claim.Status);
        Assert.Equal(RegisteredAt, claim.CreatedAt);
        Assert.Equal(RegisteredAt, claim.UpdatedAt);
    }
}
