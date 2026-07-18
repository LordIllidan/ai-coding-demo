using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Api.Controllers;

/// <summary>Mobile "me" endpoints — the caller's identity comes exclusively from the
/// authenticated JWT (subject/customerId claim). Requests never carry customerId, policyId
/// or claimId; no layer below this controller may accept a client-supplied identifier
/// as a substitute.</summary>
[ApiController]
[Route("api/mobile/me/claims")]
[Authorize]
public sealed class MobileClaimsController : ControllerBase
{
    private readonly ClaimPayoutService _claimPayoutService;

    /// <param name="claimPayoutService">Use case backing the last-payout read.</param>
    public MobileClaimsController(ClaimPayoutService claimPayoutService) => _claimPayoutService = claimPayoutService;

    /// <summary>Returns the logged-in customer's most recently paid claim installment.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 with the <see cref="LastPayoutDto"/> on success; 403 FORBIDDEN_CROSS_CUSTOMER if the
    /// token carries no resolvable customer identity; 404 LAST_PAYOUT_NOT_FOUND if the customer
    /// has no paid installment; 503 DATA_SOURCE_TIMEOUT if the data source times out.
    /// </returns>
    [HttpGet("last-payout")]
    public async Task<ActionResult<LastPayoutDto>> GetLastPayout(CancellationToken ct)
    {
        if (!TryGetCustomerId(out var customerId))
        {
            return Problem(
                "The authenticated token is not authorized for any customer's claim data.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "FORBIDDEN_CROSS_CUSTOMER");
        }

        try
        {
            var payout = await _claimPayoutService.GetLastPayoutForCustomerAsync(customerId, ct);
            if (payout is null)
            {
                return Problem(
                    "No paid claim installment was found for the logged-in customer.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "LAST_PAYOUT_NOT_FOUND");
            }

            return Ok(payout);
        }
        catch (TimeoutException)
        {
            return Problem(
                "The claim payout data source timed out. Please try again shortly.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "DATA_SOURCE_TIMEOUT");
        }
    }

    /// <summary>Reads the customer identity from the token's "customerId" claim, falling
    /// back to the standard "sub" claim — never from request path/query/body.</summary>
    private bool TryGetCustomerId(out Guid customerId)
    {
        var raw = User.FindFirstValue("customerId")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(raw, out customerId);
    }
}
