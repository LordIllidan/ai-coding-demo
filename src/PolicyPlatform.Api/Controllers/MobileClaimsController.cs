using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Mobile;

namespace PolicyPlatform.Api.Controllers;

/// <summary>Read-only endpoints for the mobile app. The authenticated customer is always
/// resolved from the JWT subject — these endpoints never accept a client-supplied
/// customerId/policyId/claimId, so there is no identifier for a client to tamper with.</summary>
[ApiController]
[Authorize]
[Route("api/mobile/me/claims")]
public sealed class MobileClaimsController : ControllerBase
{
    private readonly MobileClaimPayoutService _payouts;
    private readonly ILogger<MobileClaimsController> _logger;

    public MobileClaimsController(MobileClaimPayoutService payouts, ILogger<MobileClaimsController> logger)
    {
        _payouts = payouts;
        _logger = logger;
    }

    /// <summary>Returns the current customer's most recently paid claim payout. Read-only —
    /// the response always carries <c>readOnly: true</c> and there is no corresponding
    /// write endpoint.</summary>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>200 with the last payout; 401 if the token has no resolvable customer identity;
    /// 404 if the customer has no paid payout; 503 if the data source is unavailable/timed out;
    /// 500 on an unexpected failure.</returns>
    [HttpGet("last-payout")]
    public async Task<ActionResult<LastPayoutResponse>> GetLastPayout(CancellationToken ct)
    {
        if (!TryGetCustomerId(out var customerId))
        {
            return Error(StatusCodes.Status401Unauthorized, MobileErrorCodes.AuthRequired,
                "The token does not contain a resolvable customer identity.");
        }

        try
        {
            var response = await _payouts.GetLastPayoutAsync(customerId, ct);
            return Ok(response);
        }
        catch (LastPayoutNotFoundException)
        {
            return Error(StatusCodes.Status404NotFound, MobileErrorCodes.LastPayoutNotFound,
                "No paid claim payout was found for this customer.");
        }
        catch (DataSourceUnavailableException)
        {
            return Error(StatusCodes.Status503ServiceUnavailable, MobileErrorCodes.DataSourceTimeout,
                "The data source timed out. Please try again shortly.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while fetching the last claim payout.");
            return Error(StatusCodes.Status500InternalServerError, MobileErrorCodes.InternalError,
                "An unexpected error occurred.");
        }
    }

    private ObjectResult Error(int statusCode, string code, string message)
        => StatusCode(statusCode, new { error = code, message });

    private bool TryGetCustomerId(out Guid customerId)
    {
        var value = User.FindFirstValue("customerId")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(value, out customerId);
    }
}
