using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.LoginHistory;

public sealed class LoginHistoryEntry : Entity
{
    public Guid UserId { get; }
    public DateTimeOffset OccurredAt { get; }
    public string? DeviceLabel { get; }
    public DeviceType DeviceType { get; }
    public string? OsName { get; }
    public string? OsVersion { get; }
    public Guid? SessionId { get; }
    public string? IpAddress { get; }
    public DateTimeOffset CreatedAt { get; }

    private LoginHistoryEntry(
        Guid id,
        Guid userId,
        DateTimeOffset occurredAt,
        DeviceType deviceType,
        string? deviceLabel,
        string? osName,
        string? osVersion,
        Guid? sessionId,
        string? ipAddress,
        DateTimeOffset createdAt) : base(id)
    {
        UserId = userId;
        OccurredAt = occurredAt;
        DeviceType = deviceType;
        DeviceLabel = deviceLabel;
        OsName = osName;
        OsVersion = osVersion;
        SessionId = sessionId;
        IpAddress = ipAddress;
        CreatedAt = createdAt;
    }

    public static LoginHistoryEntry Create(
        Guid id,
        Guid userId,
        DateTimeOffset occurredAt,
        DeviceType deviceType,
        string? deviceLabel = null,
        string? osName = null,
        string? osVersion = null,
        Guid? sessionId = null,
        string? ipAddress = null,
        DateTimeOffset? createdAt = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Login history entry requires a user id.");
        }

        return new LoginHistoryEntry(
            id,
            userId,
            occurredAt,
            deviceType,
            deviceLabel,
            osName,
            osVersion,
            sessionId,
            ipAddress,
            createdAt ?? DateTimeOffset.UtcNow);
    }
}
