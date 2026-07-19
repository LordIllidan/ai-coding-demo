using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Notifications;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime in-memory store. Swap for an EF Core provider (table/index per
/// the AISDLC-156 contract) once notifications need durable persistence.</summary>
public sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<Guid, Notification> _notifications = new();

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_notifications.GetValueOrDefault(id));

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(_notifications.Values.Count(n => n.UserId == userId && !n.IsRead));

    public Task<(IReadOnlyList<Notification> Items, string? NextCursor)> GetUnreadAsync(
        Guid userId, int limit, string? cursor, CancellationToken ct = default)
    {
        var ordered = _notifications.Values
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .AsEnumerable();

        if (cursor is not null)
        {
            var (afterCreatedAt, afterId) = DecodeCursor(cursor);
            ordered = ordered.SkipWhile(n => n.CreatedAt > afterCreatedAt
                || (n.CreatedAt == afterCreatedAt && n.Id.CompareTo(afterId) >= 0));
        }

        var page = ordered.Take(limit + 1).ToList();
        var hasMore = page.Count > limit;
        var items = page.Take(limit).ToList();
        var nextCursor = hasMore ? EncodeCursor(items[^1]) : null;

        return Task.FromResult<(IReadOnlyList<Notification>, string?)>((items, nextCursor));
    }

    public Task SaveAsync(Notification notification, CancellationToken ct = default)
    {
        _notifications[notification.Id] = notification;
        return Task.CompletedTask;
    }

    private static string EncodeCursor(Notification notification)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{notification.CreatedAt.Ticks}:{notification.Id}"));

    private static (DateTime CreatedAt, Guid Id) DecodeCursor(string cursor)
    {
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(':', 2);
            var ticks = long.Parse(parts[0], CultureInfo.InvariantCulture);
            var id = Guid.Parse(parts[1]);
            return (new DateTime(ticks, DateTimeKind.Utc), id);
        }
        catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or OverflowException)
        {
            throw new ArgumentException("Invalid cursor.", nameof(cursor));
        }
    }
}
