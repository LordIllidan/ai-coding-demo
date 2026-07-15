using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Policies;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class PolicyTests
{
    private static Policy CreateDraftPolicy() => Policy.CreateDraft(
        Guid.NewGuid(),
        new PolicyNumber("POL-2026-000001"),
        Guid.NewGuid(),
        new DateOnly(2026, 1, 1),
        new DateOnly(2026, 12, 31));

    [Fact]
    public void CreateDraft_WithExpiryBeforeEffectiveDate_Throws()
    {
        Assert.Throws<DomainException>(() => Policy.CreateDraft(
            Guid.NewGuid(), new PolicyNumber("POL-2026-000002"), Guid.NewGuid(),
            new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Activate_WithoutOcCoverage_Throws()
    {
        var policy = CreateDraftPolicy();
        policy.AddCoverage(new Coverage(CoverageType.AC, new Money(80000, "PLN"), new Money(1200, "PLN")));

        var ex = Assert.Throws<DomainException>(policy.Activate);
        Assert.Contains("OC", ex.Message);
    }

    [Fact]
    public void Activate_WithOcCoverage_SetsStatusActive()
    {
        var policy = CreateDraftPolicy();
        policy.AddCoverage(new Coverage(CoverageType.OC, new Money(50000, "PLN"), new Money(800, "PLN")));

        policy.Activate();

        Assert.Equal(PolicyStatus.Active, policy.Status);
    }

    [Fact]
    public void Activate_TwiceInARow_ThrowsOnSecondCall()
    {
        var policy = CreateDraftPolicy();
        policy.AddCoverage(new Coverage(CoverageType.OC, new Money(50000, "PLN"), new Money(800, "PLN")));
        policy.Activate();

        Assert.Throws<DomainException>(policy.Activate);
    }

    [Fact]
    public void AddCoverage_DuplicateType_Throws()
    {
        var policy = CreateDraftPolicy();
        policy.AddCoverage(new Coverage(CoverageType.OC, new Money(50000, "PLN"), new Money(800, "PLN")));

        Assert.Throws<DomainException>(() =>
            policy.AddCoverage(new Coverage(CoverageType.OC, new Money(50000, "PLN"), new Money(900, "PLN"))));
    }

    [Fact]
    public void AddCoverage_AfterActivation_Throws()
    {
        var policy = CreateDraftPolicy();
        policy.AddCoverage(new Coverage(CoverageType.OC, new Money(50000, "PLN"), new Money(800, "PLN")));
        policy.Activate();

        Assert.Throws<DomainException>(() =>
            policy.AddCoverage(new Coverage(CoverageType.AC, new Money(80000, "PLN"), new Money(1200, "PLN"))));
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var policy = CreateDraftPolicy();
        policy.AddCoverage(new Coverage(CoverageType.OC, new Money(50000, "PLN"), new Money(800, "PLN")));
        policy.Activate();
        policy.Cancel();

        Assert.Throws<DomainException>(policy.Cancel);
    }

    [Fact]
    public void TotalPremium_SumsAllCoveragePremiums()
    {
        var policy = CreateDraftPolicy();
        policy.AddCoverage(new Coverage(CoverageType.OC, new Money(50000, "PLN"), new Money(800, "PLN")));
        policy.AddCoverage(new Coverage(CoverageType.AC, new Money(80000, "PLN"), new Money(1200, "PLN")));

        Assert.Equal(2000m, policy.TotalPremium.Amount);
        Assert.Equal("PLN", policy.TotalPremium.Currency);
    }

    [Fact]
    public void ExpireIfDue_PastExpiryDate_MarksExpired()
    {
        var policy = CreateDraftPolicy();
        policy.AddCoverage(new Coverage(CoverageType.OC, new Money(50000, "PLN"), new Money(800, "PLN")));
        policy.Activate();

        policy.ExpireIfDue(new DateOnly(2027, 1, 1));

        Assert.Equal(PolicyStatus.Expired, policy.Status);
    }
}
