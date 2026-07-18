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

    /// <summary>Creates the controller with its last-payout use-case dependency.</summary>
    /// <param name="service">Use-case that resolves the caller's identity and fetches their last payout.</param>
    public MobileLastPayoutController(LastPayoutService service) => _service = service;

    /// <summary>Returns the authenticated customer's last paid claim payout. Read-only: no
    /// request body or query parameters are accepted, and the customer is identified solely
    /// from the bearer token.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with the payout on success; 401/403/404/503 error envelopes on failure per
    /// <see cref="LastPayoutErrorMapper"/>.</returns>
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
