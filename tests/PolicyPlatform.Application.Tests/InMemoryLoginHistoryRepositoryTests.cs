using PolicyPlatform.Domain.LoginHistory;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class InMemoryLoginHistoryRepositoryTests
{
    private static LoginHistoryEntry CreateEntry(Guid userId, DateTimeOffset occurredAt)
        => LoginHistoryEntry.Create(Guid.NewGuid(), userId, occurredAt, DeviceType.PHONE);

    [Fact]
    public async Task GetByUserIdAsync_UnknownUser_ReturnsEmpty()
    {
        var repo = new InMemoryLoginHistoryRepository();

        var result = await repo.GetByUserIdAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyEntriesForRequestedUser()
    {
        var repo = new InMemoryLoginHistoryRepository();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var entry = CreateEntry(userId, DateTimeOffset.UtcNow);
        var otherEntry = CreateEntry(otherUserId, DateTimeOffset.UtcNow);

        await repo.AddAsync(entry);
        await repo.AddAsync(otherEntry);

        var result = await repo.GetByUserIdAsync(userId);

        var single = Assert.Single(result);
        Assert.Equal(entry.Id, single.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_OrdersEntriesNewestFirst()
    {
        var repo = new InMemoryLoginHistoryRepository();
        var userId = Guid.NewGuid();
        var oldest = CreateEntry(userId, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var middle = CreateEntry(userId, new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var newest = CreateEntry(userId, new DateTimeOffset(2026, 7, 19, 0, 0, 0, TimeSpan.Zero));

        await repo.AddAsync(middle);
        await repo.AddAsync(oldest);
        await repo.AddAsync(newest);

        var result = await repo.GetByUserIdAsync(userId);

        Assert.Equal([newest.Id, middle.Id, oldest.Id], result.Select(e => e.Id));
    }
}
