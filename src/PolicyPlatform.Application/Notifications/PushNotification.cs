namespace PolicyPlatform.Application.Notifications;

public sealed record PushNotification(string Title, string Body, IReadOnlyDictionary<string, string> Data);
