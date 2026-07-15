using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Controllers;

/// <summary>Lets a client (notably the mobile app) initiate a damage claim
/// (zgłoszenie szkody) directly against the backend, so it can start the process
/// without redirecting to a browser-hosted webview flow.</summary>
[ApiController]
[Route("api/claims")]
public sealed class ClaimsController : ControllerBase
{
    private readonly ClaimService _claimService;

    public ClaimsController(ClaimService claimService) => _claimService = claimService;

    [HttpPost]
    public async Task<ActionResult<ClaimDto>> Initiate(InitiateClaimRequest request, CancellationToken ct)
    {
        try
        {
            var claim = await _claimService.InitiateClaimAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClaimDto>> GetById(Guid id, CancellationToken ct)
    {
        var claim = await _claimService.GetClaimAsync(id, ct);
        return claim is null ? NotFound() : Ok(claim);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClaimDto>>> List(CancellationToken ct)
        => Ok(await _claimService.ListClaimsAsync(ct));
}
