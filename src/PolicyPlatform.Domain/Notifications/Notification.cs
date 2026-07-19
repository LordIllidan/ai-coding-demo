using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Notifications;

public sealed class Notification : Entity
{
    public Guid UserId { get; }
    public string Title { get; }
    public string Body { get; }
    public string Type { get; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private Notification(
        Guid id, Guid userId, string title, string body, string type,
        bool isRead, DateTime? readAt, DateTime createdAt, DateTime updatedAt)
        : base(id)
    {
        UserId = userId;
        Title = title;
        Body = body;
        Type = type;
        IsRead = isRead;
        ReadAt = readAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Notification Create(
        Guid id, Guid userId, string title, string body, string type, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Notification title cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Notification body cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new DomainException("Notification type cannot be empty.");
        }

        return new Notification(id, userId, title, body, type, isRead: false, readAt: null, createdAt, createdAt);
    }

    public void MarkAsRead(DateTime readAt)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = readAt;
        UpdatedAt = readAt;
    }
}
