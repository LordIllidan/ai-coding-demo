namespace PolicyPlatform.Infrastructure.Notifications;

/// <summary>Bound from the "PushNotifications" configuration section.</summary>
public sealed class PushNotificationOptions
{
    public const string SectionName = "PushNotifications";

    public bool Enabled { get; set; } = true;

    public string Provider { get; set; } = "Log";
}
