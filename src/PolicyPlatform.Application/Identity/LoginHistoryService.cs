using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Application.Identity;

/// <summary>Application service (use-case layer). Orchestrates domain objects and
/// repositories; contains no business rules itself — those live in the Domain.</summary>
public sealed class LoginHistoryService
{
    private readonly ILoginHistoryRepository _loginHistory;

    public LoginHistoryService(ILoginHistoryRepository loginHistory) => _loginHistory = loginHistory;

    public async Task<LoginHistoryResponse> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var entries = await _loginHistory.ListForUserAsync(userId, ct);
        var items = entries
            .OrderByDescending(e => e.OccurredAt)
            .Select(LoginHistoryEntryDto.FromDomain)
            .ToList();

        return new LoginHistoryResponse(items);
    }
}
