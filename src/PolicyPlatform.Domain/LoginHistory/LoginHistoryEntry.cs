using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.LoginHistory;

/// <summary>A single recorded successful login event for a user, used to render login history.</summary>
public sealed class LoginHistoryEntry : Entity
{
    /// <summary>Id of the user who logged in.</summary>
    public Guid UserId { get; }

    /// <summary>UTC timestamp when the login occurred.</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>Human-readable device label, if known (e.g. "iPhone 15").</summary>
    public string? DeviceLabel { get; }

    /// <summary>Category of the client device used to log in.</summary>
    public DeviceType DeviceType { get; }

    /// <summary>Operating system name, if known.</summary>
    public string? OsName { get; }

    /// <summary>Operating system version, if known.</summary>
    public string? OsVersion { get; }

    /// <summary>Id of the session created by this login, if applicable.</summary>
    public Guid? SessionId { get; }

    /// <summary>IP address the login originated from, if known.</summary>
    public string? IpAddress { get; }

    /// <summary>UTC timestamp when this record was created.</summary>
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

    /// <summary>Creates a new <see cref="LoginHistoryEntry"/>.</summary>
    /// <param name="id">Unique id of the entry.</param>
    /// <param name="userId">Id of the user who logged in. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="occurredAt">UTC timestamp when the login occurred.</param>
    /// <param name="deviceType">Category of the client device used to log in.</param>
    /// <param name="deviceLabel">Human-readable device label, if known.</param>
    /// <param name="osName">Operating system name, if known.</param>
    /// <param name="osVersion">Operating system version, if known.</param>
    /// <param name="sessionId">Id of the session created by this login, if applicable.</param>
    /// <param name="ipAddress">IP address the login originated from, if known.</param>
    /// <param name="createdAt">UTC timestamp when the record was created; defaults to now.</param>
    /// <returns>The created <see cref="LoginHistoryEntry"/>.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="userId"/> is <see cref="Guid.Empty"/>.</exception>
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
