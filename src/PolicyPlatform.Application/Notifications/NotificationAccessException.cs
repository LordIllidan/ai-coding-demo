namespace PolicyPlatform.Application.Notifications;

public enum NotificationAccessError
{
    NotFound,
    Forbidden,
}

/// <summary>Api layer maps this to 404 NOTIFICATION_NOT_FOUND / 403 FORBIDDEN.</summary>
public sealed class NotificationAccessException : Exception
{
    public NotificationAccessError Error { get; }

    public NotificationAccessException(NotificationAccessError error) => Error = error;
}
