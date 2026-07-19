using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Auth;

public sealed class LoginHistoryEntry : Entity
{
    public Guid UserId { get; }
    public DateTimeOffset OccurredAt { get; }
    public string? DeviceLabel { get; }
    public LoginDeviceType DeviceType { get; }
    public string? OsName { get; }
    public string? OsVersion { get; }
    public Guid? SessionId { get; }
    public string? IpAddress { get; }
    public DateTimeOffset CreatedAt { get; }

    private LoginHistoryEntry(
        Guid id,
        Guid userId,
        DateTimeOffset occurredAt,
        string? deviceLabel,
        LoginDeviceType deviceType,
        string? osName,
        string? osVersion,
        Guid? sessionId,
        string? ipAddress,
        DateTimeOffset createdAt) : base(id)
    {
        UserId = userId;
        OccurredAt = occurredAt;
        DeviceLabel = deviceLabel;
        DeviceType = deviceType;
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
        LoginDeviceType deviceType,
        string? deviceLabel = null,
        string? osName = null,
        string? osVersion = null,
        Guid? sessionId = null,
        string? ipAddress = null,
        DateTimeOffset? createdAt = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Login history entry user id cannot be empty.");
        }

        return new LoginHistoryEntry(
            id,
            userId,
            occurredAt,
            deviceLabel,
            deviceType,
            osName,
            osVersion,
            sessionId,
            ipAddress,
            createdAt ?? DateTimeOffset.UtcNow);
    }
}
