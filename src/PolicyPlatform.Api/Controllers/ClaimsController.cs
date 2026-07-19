using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Controllers;

/// <summary>API surface for registering and retrieving vehicle theft claims (AISDLC-51 contract).</summary>
[ApiController]
[Route("api/theft-claims")]
public sealed class ClaimsController : ControllerBase
{
    private readonly ClaimService _claimService;

    public ClaimsController(ClaimService claimService) => _claimService = claimService;

    /// <summary>Registers a new theft claim, validating the police report number.</summary>
    /// <param name="request">Policy id and police report number to register.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>201 Created</c> with the registered claim on success; <c>422 Unprocessable Entity</c>
    /// with a <see cref="ValidationErrorResponse"/> if <c>policeReportNumber</c> is missing or
    /// invalid; <c>400 Bad Request</c> if the referenced policy does not exist.
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<TheftClaimDto>> Create(CreateTheftClaimRequest request, CancellationToken ct)
    {
        try
        {
            var claim = await _claimService.RegisterTheftClaimAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = claim.ClaimId }, claim);
        }
        catch (TheftClaimValidationException ex)
        {
            return UnprocessableEntity(new ValidationErrorResponse("VALIDATION_ERROR", ex.FieldErrors));
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Retrieves a previously registered theft claim by id.</summary>
    /// <param name="id">Identifier of the claim.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>200 OK</c> with the claim, or <c>404 Not Found</c> if no such claim exists.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TheftClaimDto>> GetById(Guid id, CancellationToken ct)
    {
        var claim = await _claimService.GetTheftClaimAsync(id, ct);
        return claim is null ? NotFound() : Ok(claim);
    }
}
