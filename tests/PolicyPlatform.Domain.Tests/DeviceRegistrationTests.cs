using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Notifications;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class DeviceRegistrationTests
{
    [Fact]
    public void Register_EmptyCustomerId_Throws()
    {
        Assert.Throws<DomainException>(() =>
            DeviceRegistration.Register(Guid.NewGuid(), Guid.Empty, "token", DevicePlatform.Ios, true));
    }

    [Fact]
    public void Register_BlankPushToken_Throws()
    {
        Assert.Throws<DomainException>(() =>
            DeviceRegistration.Register(Guid.NewGuid(), Guid.NewGuid(), "  ", DevicePlatform.Android, true));
    }

    [Fact]
    public void Register_PermissionNotGranted_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            DeviceRegistration.Register(Guid.NewGuid(), Guid.NewGuid(), "token", DevicePlatform.Ios, false));
        Assert.Contains("permission", ex.Message);
    }

    [Fact]
    public void Register_ValidInput_TrimsTokenAndSetsActive()
    {
        var device = DeviceRegistration.Register(
            Guid.NewGuid(), Guid.NewGuid(), "  push-token  ", DevicePlatform.Android, true);

        Assert.Equal("push-token", device.PushToken);
        Assert.Equal(DevicePlatform.Android, device.Platform);
        Assert.True(device.NotificationsPermissionGranted);
        Assert.True(device.IsActive);
    }

    [Fact]
    public void Revoke_SetsIsActiveFalse()
    {
        var device = DeviceRegistration.Register(Guid.NewGuid(), Guid.NewGuid(), "token", DevicePlatform.Ios, true);

        device.Revoke();

        Assert.False(device.IsActive);
    }
}
