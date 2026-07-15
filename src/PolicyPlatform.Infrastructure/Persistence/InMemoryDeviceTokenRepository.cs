using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Notifications;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime in-memory store. Swap for a real database once mobile device
/// registration needs to survive a restart — the Application layer only depends on
/// IDeviceTokenRepository.</summary>
public sealed class InMemoryDeviceTokenRepository : IDeviceTokenRepository
{
    private readonly ConcurrentDictionary<string, DeviceToken> _tokens = new();

    public Task RegisterAsync(DeviceToken token, CancellationToken ct = default)
    {
        _tokens[token.Token] = token;
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(string token, CancellationToken ct = default)
    {
        _tokens.TryRemove(token, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DeviceToken>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DeviceToken>>(
            _tokens.Values.Where(t => t.CustomerId == customerId).ToList());
}
