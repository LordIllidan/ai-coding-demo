using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Api.ErrorHandling;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Api.Controllers;

/// <summary>REST endpoint for a claim's last paid tranche.</summary>
[ApiController]
[Route("api/claims")]
public sealed class ClaimTranchesController : ControllerBase
{
    private readonly ClaimLastPaidTrancheService _service;

    public ClaimTranchesController(ClaimLastPaidTrancheService service) => _service = service;

    /// <summary>Gets the last paid tranche for the given claim. Never serves cached/stale
    /// data: any downstream failure (timeout, circuit breaker, auth, not-found) is mapped
    /// to the shared error envelope instead of falling back to a previous result.</summary>
    /// <param name="claimId">Claim identifier (UUID). The only identifier used for lookup and
    /// authorization — customerId/policyId are not accepted.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>200 with <see cref="LastPaidTrancheResult"/> (lastPaidTranche is null when
    /// there is no tranche yet); 401/403/404/503/504 with an <see cref="ErrorEnvelope"/> on
    /// failure.</returns>
    [HttpGet("{claimId:guid}/last-paid-tranche")]
    public async Task<IActionResult> GetLastPaidTranche(Guid claimId, CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var authorizationHeaderValue = Request.Headers.Authorization.ToString();
            var result = await _service.GetLastPaidTrancheAsync(claimId, authorizationHeaderValue, ct);
            return Ok(result);
        }
        catch (Exception ex) when (ex is InvalidTokenException or ClaimAccessDeniedException or ClaimNotFoundException
            or TrancheServiceUnavailableException or TrancheServiceTimeoutException)
        {
            var (statusCode, envelope) = TrancheFetchErrorMapper.Map(ex, correlationId);
            return StatusCode(statusCode, envelope);
        }
    }
}
