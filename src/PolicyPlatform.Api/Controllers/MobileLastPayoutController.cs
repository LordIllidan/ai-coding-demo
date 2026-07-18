using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Api.ErrorHandling;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Api.Controllers;

/// <summary>Mobile "my last payout" screen — read-only. Only GET is mapped for this route: no
/// POST/PUT/PATCH exists here by design, so any write attempt gets ASP.NET's default 405/404
/// rather than reaching application logic.</summary>
[ApiController]
[Route("api/mobile/me/claims/last-payout")]
public sealed class MobileLastPayoutController : ControllerBase
{
    private readonly LastPayoutService _service;

    public MobileLastPayoutController(LastPayoutService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetLastPayout(CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var authorizationHeaderValue = Request.Headers.Authorization.ToString();
            var result = await _service.GetLastPayoutAsync(authorizationHeaderValue, ct);
            return Ok(result);
        }
        catch (Exception ex) when (ex is AuthRequiredException or ForbiddenCrossCustomerException
            or LastPayoutNotFoundException or DataSourceTimeoutException)
        {
            var (statusCode, envelope) = LastPayoutErrorMapper.Map(ex, correlationId);
            return StatusCode(statusCode, envelope);
        }
    }
}
