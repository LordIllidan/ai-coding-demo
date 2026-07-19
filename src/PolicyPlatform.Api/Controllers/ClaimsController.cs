using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Controllers;

/// <summary>API surface for registering and reading vehicle theft claims.</summary>
[ApiController]
[Route("api/theft-claims")]
public sealed class ClaimsController : ControllerBase
{
    private readonly ClaimService _claimService;

    public ClaimsController(ClaimService claimService) => _claimService = claimService;

    /// <summary>Registers a new theft claim, validating and normalizing the police report number.</summary>
    /// <param name="request">Claim details, including the raw police report number.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>
    /// 201 Created with the registered <see cref="TheftClaimDto"/> on success; 422 Unprocessable Entity
    /// with a <c>VALIDATION_ERROR</c> body when the police report number is missing or malformed; 400 Bad
    /// Request for other domain errors (e.g. an unknown policy).
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<TheftClaimDto>> Create(CreateTheftClaimRequest request, CancellationToken ct)
    {
        try
        {
            var claim = await _claimService.RegisterTheftClaimAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = claim.ClaimId }, claim);
        }
        catch (FieldValidationException ex)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new
            {
                code = "VALIDATION_ERROR",
                fieldErrors = new[] { new { field = ex.Field, code = ex.Code, message = ex.Message } },
            });
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TheftClaimDto>> GetById(Guid id, CancellationToken ct)
    {
        var claim = await _claimService.GetTheftClaimAsync(id, ct);
        return claim is null ? NotFound() : Ok(claim);
    }
}
