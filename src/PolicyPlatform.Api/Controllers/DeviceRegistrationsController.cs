using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Notifications;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Controllers;

[ApiController]
[Route("api/device-registrations")]
public sealed class DeviceRegistrationsController : ControllerBase
{
    private readonly DeviceRegistrationService _deviceRegistrations;

    public DeviceRegistrationsController(DeviceRegistrationService deviceRegistrations)
        => _deviceRegistrations = deviceRegistrations;

    [HttpPost]
    public async Task<ActionResult<DeviceRegistrationDto>> Register(RegisterDeviceRequest request, CancellationToken ct)
    {
        try
        {
            var device = await _deviceRegistrations.RegisterDeviceAsync(request, ct);
            return Ok(device);
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("{id:guid}/unregister")]
    public async Task<ActionResult<DeviceRegistrationDto>> Unregister(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _deviceRegistrations.UnregisterDeviceAsync(id, ct));
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
