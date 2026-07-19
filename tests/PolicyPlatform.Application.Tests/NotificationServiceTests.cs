using PolicyPlatform.Application.Notifications;
using PolicyPlatform.Domain.Notifications;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class NotificationServiceTests
{
    private static (NotificationService Service, InMemoryNotificationRepository Repository) CreateService()
    {
        var repository = new InMemoryNotificationRepository();
        return (new NotificationService(repository), repository);
    }

    private static async Task<Notification> SeedAsync(
        InMemoryNotificationRepository repository, Guid userId, DateTime createdAt, bool isRead = false)
    {
        var notification = Notification.Create(
            Guid.NewGuid(), userId, "Tytul", "Tresc powiadomienia.", "policy.created", createdAt);
        if (isRead)
        {
            notification.MarkAsRead(createdAt.AddMinutes(1));
        }

        await repository.SaveAsync(notification);
        return notification;
    }

    [Fact]
    public async Task GetCounter_NoNotifications_ReturnsZeroExplicitly()
    {
        var (service, _) = CreateService();

        var result = await service.GetCounterAsync(Guid.NewGuid());

        Assert.Equal(0, result.UnreadCount);
    }

    [Fact]
    public async Task GetCounter_OnlyCountsUnreadForCurrentUser()
    {
        var (service, repository) = CreateService();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await SeedAsync(repository, userId, now);
        await SeedAsync(repository, userId, now, isRead: true);
        await SeedAsync(repository, otherUserId, now);

        var result = await service.GetCounterAsync(userId);

        Assert.Equal(1, result.UnreadCount);
    }

    [Fact]
    public async Task GetUnread_ReturnsOnlyUnreadItemsForCurrentUser_NewestFirst()
    {
        var (service, repository) = CreateService();
        var userId = Guid.NewGuid();
        var older = await SeedAsync(repository, userId, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = await SeedAsync(repository, userId, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        await SeedAsync(repository, userId, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc), isRead: true);
        await SeedAsync(repository, Guid.NewGuid(), new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc));

        var result = await service.GetUnreadAsync(userId, limit: 50, cursor: null);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(newer.Id, result.Items[0].Id);
        Assert.Equal(older.Id, result.Items[1].Id);
        Assert.All(result.Items, item => Assert.False(item.IsRead));
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task GetUnread_LimitSmallerThanResults_ReturnsNextCursor()
    {
        var (service, repository) = CreateService();
        var userId = Guid.NewGuid();
        await SeedAsync(repository, userId, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedAsync(repository, userId, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        var firstPage = await service.GetUnreadAsync(userId, limit: 1, cursor: null);
        Assert.Single(firstPage.Items);
        Assert.NotNull(firstPage.NextCursor);

        var secondPage = await service.GetUnreadAsync(userId, limit: 1, cursor: firstPage.NextCursor);
        Assert.Single(secondPage.Items);
        Assert.NotEqual(firstPage.Items[0].Id, secondPage.Items[0].Id);
        Assert.Null(secondPage.NextCursor);
    }

    [Fact]
    public async Task MarkAsRead_OwnedNotification_MarksReadAndReturnsUpdatedCounter()
    {
        var (service, repository) = CreateService();
        var userId = Guid.NewGuid();
        var notification = await SeedAsync(repository, userId, DateTime.UtcNow);

        var result = await service.MarkAsReadAsync(userId, notification.Id);

        Assert.Equal(notification.Id, result.NotificationId);
        Assert.True(result.IsRead);
        Assert.Equal(0, result.UnreadCount);
    }

    [Fact]
    public async Task MarkAsRead_AlreadyRead_IsIdempotentAndReturnsCurrentCounter()
    {
        var (service, repository) = CreateService();
        var userId = Guid.NewGuid();
        var notification = await SeedAsync(repository, userId, DateTime.UtcNow);
        var firstResult = await service.MarkAsReadAsync(userId, notification.Id);

        var secondResult = await service.MarkAsReadAsync(userId, notification.Id);

        Assert.True(secondResult.IsRead);
        Assert.Equal(firstResult.ReadAt, secondResult.ReadAt);
        Assert.Equal(0, secondResult.UnreadCount);
    }

    [Fact]
    public async Task MarkAsRead_UnknownNotification_ThrowsNotFound()
    {
        var (service, _) = CreateService();

        var ex = await Assert.ThrowsAsync<NotificationAccessException>(
            () => service.MarkAsReadAsync(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(NotificationAccessError.NotFound, ex.Error);
    }

    [Fact]
    public async Task MarkAsRead_BelongsToAnotherUser_ThrowsForbidden()
    {
        var (service, repository) = CreateService();
        var owner = Guid.NewGuid();
        var intruder = Guid.NewGuid();
        var notification = await SeedAsync(repository, owner, DateTime.UtcNow);

        var ex = await Assert.ThrowsAsync<NotificationAccessException>(
            () => service.MarkAsReadAsync(intruder, notification.Id));

        Assert.Equal(NotificationAccessError.Forbidden, ex.Error);
    }
}
