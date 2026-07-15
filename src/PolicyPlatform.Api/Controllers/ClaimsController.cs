using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Controllers;

[ApiController]
[Route("api/theft-claims")]
public sealed class ClaimsController : ControllerBase
{
    private readonly ClaimService _claimService;

    public ClaimsController(ClaimService claimService) => _claimService = claimService;

    [HttpPost]
    public async Task<ActionResult<TheftClaimDto>> Create(CreateTheftClaimRequest request, CancellationToken ct)
    {
        try
        {
            var claim = await _claimService.RegisterTheftClaimAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
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
