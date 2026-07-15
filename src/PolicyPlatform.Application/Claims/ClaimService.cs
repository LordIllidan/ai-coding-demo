using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Claims;

/// <summary>Application service (use-case layer) for initiating a damage claim
/// (zgłoszenie szkody) directly from a channel such as the mobile app, without
/// requiring a browser/webview handoff. Contains no business rules itself —
/// those live in the Domain.</summary>
public sealed class ClaimService
{
    private readonly IClaimRepository _claims;
    private readonly IPolicyRepository _policies;
    private readonly ICustomerRepository _customers;

    public ClaimService(IClaimRepository claims, IPolicyRepository policies, ICustomerRepository customers)
    {
        _claims = claims;
        _policies = policies;
        _customers = customers;
    }

    public async Task<ClaimDto> InitiateClaimAsync(InitiateClaimRequest request, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, ct)
            ?? throw new DomainException($"Customer {request.CustomerId} was not found.");

        var policy = await _policies.GetByIdAsync(request.PolicyId, ct)
            ?? throw new DomainException($"Policy {request.PolicyId} was not found.");

        if (policy.CustomerId != customer.Id)
        {
            throw new DomainException("Policy does not belong to the given customer.");
        }

        if (policy.Status != PolicyStatus.Active)
        {
            throw new DomainException($"A claim can only be reported against an active policy (current status: {policy.Status}).");
        }

        if (!Enum.TryParse<ClaimChannel>(request.Channel, ignoreCase: true, out var channel))
        {
            throw new DomainException($"'{request.Channel}' is not a recognized claim channel.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var claim = Claim.Initiate(
            Guid.NewGuid(), policy.Id, customer.Id, channel, request.IncidentDate, request.Description, today, DateTime.UtcNow);

        await _claims.AddAsync(claim, ct);
        return ClaimDto.FromDomain(claim);
    }

    public async Task<ClaimDto?> GetClaimAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await _claims.GetByIdAsync(claimId, ct);
        return claim is null ? null : ClaimDto.FromDomain(claim);
    }

    public async Task<IReadOnlyList<ClaimDto>> ListClaimsAsync(CancellationToken ct = default)
    {
        var claims = await _claims.ListAsync(ct);
        return claims.Select(ClaimDto.FromDomain).ToList();
    }
}
