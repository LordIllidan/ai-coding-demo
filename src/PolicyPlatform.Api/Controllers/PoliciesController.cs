using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Policies;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Controllers;

[ApiController]
[Route("api/policies")]
public sealed class PoliciesController : ControllerBase
{
    private readonly PolicyService _policyService;

    public PoliciesController(PolicyService policyService) => _policyService = policyService;

    [HttpPost]
    public async Task<ActionResult<PolicyDto>> Create(CreatePolicyRequest request, CancellationToken ct)
    {
        try
        {
            var policy = await _policyService.CreatePolicyAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = policy.Id }, policy);
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PolicyDto>> GetById(Guid id, CancellationToken ct)
    {
        var policy = await _policyService.GetPolicyAsync(id, ct);
        return policy is null ? NotFound() : Ok(policy);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PolicyDto>>> List(CancellationToken ct)
        => Ok(await _policyService.ListPoliciesAsync(ct));

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<PolicyDto>> Activate(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _policyService.ActivatePolicyAsync(id, ct));
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PolicyDto>> Cancel(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _policyService.CancelPolicyAsync(id, ct));
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
