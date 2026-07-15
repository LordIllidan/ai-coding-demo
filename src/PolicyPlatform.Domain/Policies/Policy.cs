using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Policies;

public sealed class Policy : Entity
{
    private readonly List<Coverage> _coverages = new();

    public PolicyNumber Number { get; }
    public Guid CustomerId { get; }
    public DateOnly EffectiveDate { get; }
    public DateOnly ExpiryDate { get; }
    public PolicyStatus Status { get; private set; }
    public IReadOnlyCollection<Coverage> Coverages => _coverages.AsReadOnly();

    public Money TotalPremium =>
        _coverages.Count == 0
            ? Money.Zero(Currency)
            : _coverages.Select(c => c.Premium).Aggregate((a, b) => a.Add(b));

    private string Currency => _coverages.Count > 0 ? _coverages[0].Premium.Currency : "PLN";

    private Policy(
        Guid id, PolicyNumber number, Guid customerId,
        DateOnly effectiveDate, DateOnly expiryDate, PolicyStatus status)
        : base(id)
    {
        Number = number;
        CustomerId = customerId;
        EffectiveDate = effectiveDate;
        ExpiryDate = expiryDate;
        Status = status;
    }

    public static Policy CreateDraft(
        Guid id, PolicyNumber number, Guid customerId, DateOnly effectiveDate, DateOnly expiryDate)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Policy must belong to a valid customer.");
        }

        if (expiryDate <= effectiveDate)
        {
            throw new DomainException("Policy expiry date must be after the effective date.");
        }

        return new Policy(id, number, customerId, effectiveDate, expiryDate, PolicyStatus.Draft);
    }

    public void AddCoverage(Coverage coverage)
    {
        if (Status != PolicyStatus.Draft)
        {
            throw new DomainException("Coverages can only be added while the policy is a draft.");
        }

        if (_coverages.Any(c => c.Type == coverage.Type))
        {
            throw new DomainException($"Coverage of type {coverage.Type} is already present on this policy.");
        }

        _coverages.Add(coverage);
    }

    public void Activate()
    {
        if (Status != PolicyStatus.Draft)
        {
            throw new DomainException($"Only a draft policy can be activated (current status: {Status}).");
        }

        if (_coverages.Count == 0)
        {
            throw new DomainException("At least one coverage is required to activate a policy.");
        }

        if (!_coverages.Any(c => c.Type == CoverageType.OC))
        {
            throw new DomainException("Mandatory OC (third-party liability) coverage is missing.");
        }

        Status = PolicyStatus.Active;
    }

    public void Cancel()
    {
        if (Status is PolicyStatus.Cancelled or PolicyStatus.Expired)
        {
            throw new DomainException($"Cannot cancel a policy that is already {Status}.");
        }

        Status = PolicyStatus.Cancelled;
    }

    public void ExpireIfDue(DateOnly today)
    {
        if (Status == PolicyStatus.Active && today > ExpiryDate)
        {
            Status = PolicyStatus.Expired;
        }
    }
}
