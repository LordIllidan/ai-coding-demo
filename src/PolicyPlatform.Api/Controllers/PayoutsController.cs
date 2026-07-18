using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Api.Controllers;

/// <summary>Exposes claim payout lookups, currently the last-paid-installment screen
/// (AISDLC-136).</summary>
[ApiController]
[Route("api/v1/claims/{claimId:guid}/payouts")]
public sealed class PayoutsController : ControllerBase
{
    private readonly PayoutService _payoutService;
    private readonly ILogger<PayoutsController> _logger;

    /// <summary>Initializes the controller with its application service and logger.</summary>
    /// <param name="payoutService">Looks up claims and their last paid installment.</param>
    /// <param name="logger">Logger used to record lookup failures.</param>
    public PayoutsController(PayoutService payoutService, ILogger<PayoutsController> logger)
    {
        _payoutService = payoutService;
        _logger = logger;
    }

    // Authorization here is limited to "is a bearer token present" — there is no JWT
    // validation/roles infrastructure in this codebase yet (see PolicyPlatform.Api Program.cs;
    // no AddAuthentication is wired up), and no claim-to-user ownership model to evaluate
    // CLAIM_ACCESS_DENIED against. Both need a dedicated auth ticket; until then this endpoint
    // only enforces the shape of the contract's 401 case and cannot yet produce a genuine 403.
    /// <summary>Returns the last paid installment for a claim, mapped to
    /// screenState PAID / NO_PAYOUT / INCOMPLETE_DATA per the AISDLC-136 contract.</summary>
    /// <param name="claimId">UUID of the claim (not customerId or policyId).</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>200 with the mapped response; 401 UNAUTHORIZED when the bearer token is
    /// missing/malformed; 404 CLAIM_NOT_FOUND when the claim does not exist; 500
    /// CLAIM_PAYOUT_LOOKUP_FAILED on lookup failure.</returns>
    [HttpGet("last-paid-installment")]
    public async Task<ActionResult<ClaimLastPaidInstallmentResponse>> GetLastPaidInstallment(
        Guid claimId, CancellationToken ct)
    {
        if (!TryGetBearerToken(out _))
        {
            return Problem(
                detail: "Missing or malformed Authorization bearer token.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?> { ["code"] = "UNAUTHORIZED" });
        }

        try
        {
            var response = await _payoutService.GetLastPaidInstallmentAsync(claimId, ct);
            if (response is null)
            {
                return Problem(
                    detail: $"Claim {claimId} was not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: new Dictionary<string, object?> { ["code"] = "CLAIM_NOT_FOUND" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to look up last paid installment for claim {ClaimId}.", claimId);
            return Problem(
                detail: "Failed to look up the claim payout.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?> { ["code"] = "CLAIM_PAYOUT_LOOKUP_FAILED" });
        }
    }

    /// <summary>Extracts the bearer token from the Authorization header.</summary>
    /// <param name="token">The extracted token, or empty when absent/malformed.</param>
    /// <returns>True when a non-empty bearer token was present.</returns>
    private bool TryGetBearerToken(out string token)
    {
        token = string.Empty;
        if (!Request.Headers.TryGetValue("Authorization", out var values))
        {
            return false;
        }

        var header = values.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = header[prefix.Length..].Trim();
        return token.Length > 0;
    }
}
