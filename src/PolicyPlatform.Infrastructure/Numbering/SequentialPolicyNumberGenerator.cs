using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Infrastructure.Numbering;

public sealed class SequentialPolicyNumberGenerator : IPolicyNumberGenerator
{
    private long _counter;

    public Task<PolicyNumber> NextAsync(CancellationToken ct = default)
    {
        var next = Interlocked.Increment(ref _counter);
        var number = new PolicyNumber($"POL-{DateTime.UtcNow:yyyy}-{next:D6}");
        return Task.FromResult(number);
    }
}
