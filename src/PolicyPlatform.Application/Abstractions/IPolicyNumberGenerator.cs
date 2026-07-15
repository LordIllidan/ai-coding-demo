using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Abstractions;

public interface IPolicyNumberGenerator
{
    Task<PolicyNumber> NextAsync(CancellationToken ct = default);
}
