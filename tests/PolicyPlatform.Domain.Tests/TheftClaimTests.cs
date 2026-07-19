using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class TheftClaimTests
{
    private static PoliceReportNumber CreateReportNumber() =>
        PoliceReportNumber.TryCreate("KMP/123/2026", out var number, out _) ? number : default;

    [Fact]
    public void Register_EmptyPolicyId_Throws()
    {
        Assert.Throws<DomainException>(() => TheftClaim.Register(
            Guid.NewGuid(), Guid.Empty, CreateReportNumber(), DateTime.UtcNow));
    }

    [Fact]
    public void Register_ValidData_SetsAllProperties()
    {
        var policyId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        var claim = TheftClaim.Register(Guid.NewGuid(), policyId, CreateReportNumber(), now);

        Assert.Equal(policyId, claim.PolicyId);
        Assert.Equal("KMP/123/2026", claim.PoliceReportNumber.Value);
        Assert.Equal(TheftClaimStatus.Accepted, claim.Status);
        Assert.Equal(now, claim.CreatedAt);
        Assert.Equal(now, claim.UpdatedAt);
    }
}
