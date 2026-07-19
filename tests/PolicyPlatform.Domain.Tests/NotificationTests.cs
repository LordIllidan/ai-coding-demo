using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Notifications;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class NotificationTests
{
    private static Notification CreateNotification(Guid userId, DateTime createdAt) => Notification.Create(
        Guid.NewGuid(), userId, "Nowa polisa", "Tresc powiadomienia.", "policy.created", createdAt);

    [Fact]
    public void Create_EmptyId_Throws()
    {
        Assert.Throws<DomainException>(() => Notification.Create(
            Guid.Empty, Guid.NewGuid(), "Tytul", "Tresc", "type", DateTime.UtcNow));
    }

    [Fact]
    public void Create_BlankTitle_Throws()
    {
        Assert.Throws<DomainException>(() => Notification.Create(
            Guid.NewGuid(), Guid.NewGuid(), "   ", "Tresc", "type", DateTime.UtcNow));
    }

    [Fact]
    public void Create_BlankBody_Throws()
    {
        Assert.Throws<DomainException>(() => Notification.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Tytul", "   ", "type", DateTime.UtcNow));
    }

    [Fact]
    public void Create_BlankType_Throws()
    {
        Assert.Throws<DomainException>(() => Notification.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Tytul", "Tresc", "   ", DateTime.UtcNow));
    }

    [Fact]
    public void Create_ValidData_StartsUnread()
    {
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var notification = CreateNotification(userId, createdAt);

        Assert.Equal(userId, notification.UserId);
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAt);
        Assert.Equal(createdAt, notification.CreatedAt);
        Assert.Equal(createdAt, notification.UpdatedAt);
    }

    [Fact]
    public void MarkAsRead_Unread_SetsReadState()
    {
        var notification = CreateNotification(Guid.NewGuid(), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var readAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        notification.MarkAsRead(readAt);

        Assert.True(notification.IsRead);
        Assert.Equal(readAt, notification.ReadAt);
        Assert.Equal(readAt, notification.UpdatedAt);
    }

    [Fact]
    public void MarkAsRead_AlreadyRead_IsIdempotent()
    {
        var notification = CreateNotification(Guid.NewGuid(), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var firstReadAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var secondReadAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);

        notification.MarkAsRead(firstReadAt);
        notification.MarkAsRead(secondReadAt);

        Assert.True(notification.IsRead);
        Assert.Equal(firstReadAt, notification.ReadAt);
        Assert.Equal(firstReadAt, notification.UpdatedAt);
    }
}
