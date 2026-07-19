using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Identity;

public sealed class LoginHistoryEntry : Entity
{
    public Guid UserId { get; }
    public DateTime OccurredAt { get; }
    public string? DeviceLabel { get; }
    public DeviceType DeviceType { get; }
    public string? OsName { get; }
    public string? OsVersion { get; }
    public Guid? SessionId { get; }
    public string? IpAddress { get; }

    private LoginHistoryEntry(
        Guid id,
        Guid userId,
        DateTime occurredAt,
        DeviceType deviceType,
        string? deviceLabel,
        string? osName,
        string? osVersion,
        Guid? sessionId,
        string? ipAddress) : base(id)
    {
        UserId = userId;
        OccurredAt = occurredAt;
        DeviceType = deviceType;
        DeviceLabel = deviceLabel;
        OsName = osName;
        OsVersion = osVersion;
        SessionId = sessionId;
        IpAddress = ipAddress;
    }

    public static LoginHistoryEntry Create(
        Guid id,
        Guid userId,
        DateTime occurredAt,
        DeviceType deviceType,
        string? deviceLabel = null,
        string? osName = null,
        string? osVersion = null,
        Guid? sessionId = null,
        string? ipAddress = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Login history entry must belong to a user.");
        }

        return new LoginHistoryEntry(id, userId, occurredAt, deviceType, deviceLabel, osName, osVersion, sessionId, ipAddress);
    }
}
