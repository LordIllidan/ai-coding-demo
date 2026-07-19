using PolicyPlatform.Domain.Identity;

namespace PolicyPlatform.Application.Identity;

public sealed record LoginHistoryEntryDto(
    Guid LoginId,
    DateTime OccurredAt,
    string? DeviceLabel,
    string DeviceType,
    string? OsName,
    string? OsVersion,
    Guid? SessionId,
    string? IpAddress)
{
    public static LoginHistoryEntryDto FromDomain(LoginHistoryEntry entry) => new(
        entry.Id,
        entry.OccurredAt,
        entry.DeviceLabel,
        entry.DeviceType.ToString(),
        entry.OsName,
        entry.OsVersion,
        entry.SessionId,
        entry.IpAddress);
}

public sealed record LoginHistoryResponse(IReadOnlyList<LoginHistoryEntryDto> Items);
