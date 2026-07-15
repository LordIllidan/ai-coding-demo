using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

public sealed class Claim : Entity
{
    public Guid PolicyId { get; }
    public Guid CustomerId { get; }
    public ClaimChannel Channel { get; }
    public DateOnly IncidentDate { get; }
    public string? Description { get; }
    public DateTime CreatedAtUtc { get; }

    private Claim(
        Guid id, Guid policyId, Guid customerId, ClaimChannel channel,
        DateOnly incidentDate, string? description, DateTime createdAtUtc)
        : base(id)
    {
        PolicyId = policyId;
        CustomerId = customerId;
        Channel = channel;
        IncidentDate = incidentDate;
        Description = description;
        CreatedAtUtc = createdAtUtc;
    }

    public static Claim Initiate(
        Guid id, Guid policyId, Guid customerId, ClaimChannel channel,
        DateOnly incidentDate, string? description, DateOnly today, DateTime createdAtUtc)
    {
        if (policyId == Guid.Empty)
        {
            throw new DomainException("Claim must reference a valid policy.");
        }

        if (customerId == Guid.Empty)
        {
            throw new DomainException("Claim must reference a valid customer.");
        }

        if (incidentDate > today)
        {
            throw new DomainException("Incident date cannot be in the future.");
        }

        return new Claim(id, policyId, customerId, channel, incidentDate, description?.Trim(), createdAtUtc);
    }
}
