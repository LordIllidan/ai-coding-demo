using PolicyPlatform.Domain.Auth;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class InMemoryLoginHistoryRepositoryTests
{
    [Fact]
    public async Task ListForUserAsync_ReturnsOnlyEntriesForGivenUser()
    {
        var repo = new InMemoryLoginHistoryRepository();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await repo.AddAsync(LoginHistoryEntry.Create(
            Guid.NewGuid(), userId, DateTimeOffset.UtcNow, LoginDeviceType.Phone));
        await repo.AddAsync(LoginHistoryEntry.Create(
            Guid.NewGuid(), otherUserId, DateTimeOffset.UtcNow, LoginDeviceType.Web));

        var result = await repo.ListForUserAsync(userId);

        var entry = Assert.Single(result);
        Assert.Equal(userId, entry.UserId);
    }

    [Fact]
    public async Task ListForUserAsync_OrdersEntriesByOccurredAtDescending()
    {
        var repo = new InMemoryLoginHistoryRepository();
        var userId = Guid.NewGuid();
        var oldest = LoginHistoryEntry.Create(
            Guid.NewGuid(), userId, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), LoginDeviceType.Phone);
        var middle = LoginHistoryEntry.Create(
            Guid.NewGuid(), userId, new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), LoginDeviceType.Tablet);
        var newest = LoginHistoryEntry.Create(
            Guid.NewGuid(), userId, new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), LoginDeviceType.Web);
        await repo.AddAsync(middle);
        await repo.AddAsync(oldest);
        await repo.AddAsync(newest);

        var result = await repo.ListForUserAsync(userId);

        Assert.Equal([newest.Id, middle.Id, oldest.Id], result.Select(e => e.Id));
    }

    [Fact]
    public async Task ListForUserAsync_UnknownUser_ReturnsEmpty()
    {
        var repo = new InMemoryLoginHistoryRepository();

        var result = await repo.ListForUserAsync(Guid.NewGuid());

        Assert.Empty(result);
    }
}
