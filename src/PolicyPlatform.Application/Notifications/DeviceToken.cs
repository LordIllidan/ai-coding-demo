namespace PolicyPlatform.Application.Notifications;

public sealed record DeviceToken(Guid CustomerId, string Token, DevicePlatform Platform, DateTimeOffset RegisteredAtUtc);
