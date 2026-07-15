namespace PolicyPlatform.Application.Notifications;

public sealed record DeviceRegistrationRequest(Guid CustomerId, string Token, DevicePlatform Platform);
