using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Policies;

public sealed class Coverage
{
    public CoverageType Type { get; }
    public Money SumInsured { get; }
    public Money Premium { get; }

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
