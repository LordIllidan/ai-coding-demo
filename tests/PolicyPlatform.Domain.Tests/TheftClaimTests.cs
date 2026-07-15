using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class TheftClaimTests
{
    private static TheftClaim CreateClaim(Guid policyId) => TheftClaim.Register(
        Guid.NewGuid(),
        policyId,
        new DateOnly(2026, 1, 1),
        "Skradziono pojazd z parkingu.",
        new PoliceReportNumber("KMP/123/2026"),
        new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

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
        Assert.Equal(new DateOnly(2026, 1, 1), claim.IncidentDate);
        Assert.Equal("Skradziono pojazd z parkingu.", claim.Description);
        Assert.Equal("KMP/123/2026", claim.PoliceReportNumber.Value);
        Assert.Equal(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), claim.ReportedAt);
    }
}
