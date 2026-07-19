using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.LoginHistory;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class LoginHistoryEntryTests
{
    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 7, 19, 8, 30, 0, TimeSpan.Zero);
        var createdAt = new DateTimeOffset(2026, 7, 19, 8, 30, 5, TimeSpan.Zero);

        var entry = LoginHistoryEntry.Create(
            id,
            userId,
            occurredAt,
            DeviceType.PHONE,
            deviceLabel: "iPhone 15",
            osName: "iOS",
            osVersion: "17.5",
            sessionId: sessionId,
            ipAddress: "203.0.113.10",
            createdAt: createdAt);

        Assert.Equal(id, entry.Id);
        Assert.Equal(userId, entry.UserId);
        Assert.Equal(occurredAt, entry.OccurredAt);
        Assert.Equal(DeviceType.PHONE, entry.DeviceType);
        Assert.Equal("iPhone 15", entry.DeviceLabel);
        Assert.Equal("iOS", entry.OsName);
        Assert.Equal("17.5", entry.OsVersion);
        Assert.Equal(sessionId, entry.SessionId);
        Assert.Equal("203.0.113.10", entry.IpAddress);
        Assert.Equal(createdAt, entry.CreatedAt);
    }

    [Fact]
    public void Create_WithoutOptionalFields_LeavesThemNull()
    {
        var entry = LoginHistoryEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DeviceType.UNKNOWN);

        Assert.Null(entry.DeviceLabel);
        Assert.Null(entry.OsName);
        Assert.Null(entry.OsVersion);
        Assert.Null(entry.SessionId);
        Assert.Null(entry.IpAddress);
    }

    [Fact]
    public void Create_WithoutCreatedAt_DefaultsToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;

        var entry = LoginHistoryEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DeviceType.WEB);

        var after = DateTimeOffset.UtcNow;

        Assert.InRange(entry.CreatedAt, before, after);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => LoginHistoryEntry.Create(
            Guid.NewGuid(),
            Guid.Empty,
            DateTimeOffset.UtcNow,
            DeviceType.TABLET));

        Assert.Contains("user id", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyId_Throws()
    {
        Assert.Throws<DomainException>(() => LoginHistoryEntry.Create(
            Guid.Empty,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DeviceType.PHONE));
    }
}
