using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Notifications;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Controllers;

/// <summary>Registers the mobile app's push token so it can receive status-change
/// notifications for the customer's zgłoszenia (policies/claims).</summary>
[ApiController]
[Route("api/device-tokens")]
public sealed class DeviceTokensController : ControllerBase
{
    private readonly DeviceRegistrationService _deviceRegistration;

    public DeviceTokensController(DeviceRegistrationService deviceRegistration)
        => _deviceRegistration = deviceRegistration;

    [HttpPost]
    public async Task<IActionResult> Register(DeviceRegistrationRequest request, CancellationToken ct)
    {
        try
        {
            await _deviceRegistration.RegisterAsync(request, ct);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("{token}")]
    public async Task<IActionResult> Unregister(string token, CancellationToken ct)
    {
        await _deviceRegistration.UnregisterAsync(token, ct);
        return NoContent();
    }
}
