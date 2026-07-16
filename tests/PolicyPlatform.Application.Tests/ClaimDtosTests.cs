using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Claims;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class ClaimDtosTests
{
    [Fact]
    public void FromDomain_MapsClaimFieldsAndAllowsNextStep()
    {
        var claim = TheftClaim.Register(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            "Skradziono pojazd z parkingu.",
            new PoliceReportNumber("KMP/123/2026"),
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        var dto = TheftClaimDto.FromDomain(claim);

        Assert.Equal(claim.Id, dto.ClaimId);
        Assert.Equal(claim.PolicyId, dto.PolicyId);
        Assert.Equal("KMP/123/2026", dto.PoliceReportNumber);
        Assert.Equal("ACCEPTED", dto.Status);
        Assert.True(dto.NextStepAllowed);
    }
}
