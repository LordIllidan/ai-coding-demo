using PolicyPlatform.Domain.Auth;
using PolicyPlatform.Domain.Common;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class LoginHistoryEntryTests
{
    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => LoginHistoryEntry.Create(
            Guid.NewGuid(), Guid.Empty, DateTimeOffset.UtcNow, LoginDeviceType.Phone));

        Assert.Contains("user id", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyId_Throws()
    {
        Assert.Throws<DomainException>(() => LoginHistoryEntry.Create(
            Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow, LoginDeviceType.Phone));
    }

    [Fact]
    public void Create_WithValidArguments_SetsAllFields()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var createdAt = new DateTimeOffset(2026, 7, 1, 12, 5, 0, TimeSpan.Zero);

        var entry = LoginHistoryEntry.Create(
            id,
            userId,
            occurredAt,
            LoginDeviceType.Tablet,
            deviceLabel: "iPad Pro",
            osName: "iPadOS",
            osVersion: "18.1",
            sessionId: sessionId,
            ipAddress: "203.0.113.5",
            createdAt: createdAt);

        Assert.Equal(id, entry.Id);
        Assert.Equal(userId, entry.UserId);
        Assert.Equal(occurredAt, entry.OccurredAt);
        Assert.Equal(LoginDeviceType.Tablet, entry.DeviceType);
        Assert.Equal("iPad Pro", entry.DeviceLabel);
        Assert.Equal("iPadOS", entry.OsName);
        Assert.Equal("18.1", entry.OsVersion);
        Assert.Equal(sessionId, entry.SessionId);
        Assert.Equal("203.0.113.5", entry.IpAddress);
        Assert.Equal(createdAt, entry.CreatedAt);
    }

    [Fact]
    public void Create_WithoutCreatedAt_DefaultsToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;

        var entry = LoginHistoryEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, LoginDeviceType.Web);

        var after = DateTimeOffset.UtcNow;
        Assert.InRange(entry.CreatedAt, before, after);
    }

    [Fact]
    public void Create_WithoutOptionalFields_LeavesThemNull()
    {
        var entry = LoginHistoryEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, LoginDeviceType.Unknown);

        Assert.Null(entry.DeviceLabel);
        Assert.Null(entry.OsName);
        Assert.Null(entry.OsVersion);
        Assert.Null(entry.SessionId);
        Assert.Null(entry.IpAddress);
    }
}
