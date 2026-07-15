using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Policies;

public sealed class Coverage
{
    public CoverageType Type { get; private set; }
    public Money SumInsured { get; private set; } = null!;
    public Money Premium { get; private set; } = null!;

    /// <summary>For EF Core materialization only — owned-navigation properties (Money)
    /// cannot be constructor-bound, so the ORM needs a parameterless constructor and sets
    /// properties via their (private) setters afterward.</summary>
    private Coverage()
    {
    }

    public Coverage(CoverageType type, Money sumInsured, Money premium)
    {
        if (sumInsured.Amount <= 0)
        {
            throw new DomainException("Sum insured must be greater than zero.");
        }

        Type = type;
        SumInsured = sumInsured;
        Premium = premium;
    }
}
