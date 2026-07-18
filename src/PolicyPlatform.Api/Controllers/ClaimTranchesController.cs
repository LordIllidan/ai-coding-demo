using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Api.ErrorHandling;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Api.Controllers;

[ApiController]
[Route("api/claims")]
public sealed class ClaimTranchesController : ControllerBase
{
    private readonly ClaimLastPaidTrancheService _service;

    public ClaimTranchesController(ClaimLastPaidTrancheService service) => _service = service;

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
