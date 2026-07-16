using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Controllers;

[ApiController]
[Route("api/theft-claims")]
public sealed class ClaimsController : ControllerBase
{
    private readonly ClaimService _claimService;

    public ClaimsController(ClaimService claimService) => _claimService = claimService;

    /// <summary>Registers a theft claim after validating the police report number.</summary>
    /// <param name="request">Policy id and police report number to validate/normalize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 201 with the created claim on success; 422 with a <see cref="ValidationErrorResponse"/>
    /// if the police report number is missing or malformed; 400 if the policy does not exist.
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<TheftClaimCreatedResponse>> Create(CreateTheftClaimRequest request, CancellationToken ct)
    {
        try
        {
            var claim = await _claimService.RegisterTheftClaimAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = claim.ClaimId }, claim);
        }
        catch (PoliceReportNumberValidationException ex)
        {
            var error = new ValidationErrorResponse(
                "VALIDATION_ERROR",
                [new FieldError("policeReportNumber", ex.Code, PoliceReportNumberValidationException.ValidationMessage)]);
            return UnprocessableEntity(error);
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
