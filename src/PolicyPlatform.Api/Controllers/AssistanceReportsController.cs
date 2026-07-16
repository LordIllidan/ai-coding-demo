using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Assistance;
using PolicyPlatform.Domain.Assistance;

namespace PolicyPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/assistance/reports")]
public sealed class AssistanceReportsController : ControllerBase
{
    private readonly AssistanceReportService _service;
    private readonly ICurrentUserAccessor _currentUser;

    public AssistanceReportsController(AssistanceReportService service, ICurrentUserAccessor currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<ActionResult<AssistanceReportDto>> Create(
        [FromBody] CreateAssistanceReportRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || !Guid.TryParse(idempotencyKey, out var idempotencyKeyGuid))
        {
            return AssistanceError(AssistanceErrorCodes.MissingIdempotencyKey, "Idempotency-Key header (UUID v4) is required.");
        }

        try
        {
            var userId = _currentUser.GetUserId();
            var dto = await _service.RegisterAsync(userId, idempotencyKeyGuid, request, ct);
            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (AssistanceDomainException ex)
        {
            return AssistanceError(ex.Code, ex.Message);
        }
    }

    private ObjectResult AssistanceError(string code, string message)
    {
        var statusCode = code switch
        {
            AssistanceErrorCodes.DuplicateSubmission => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return StatusCode(statusCode, new { code, message });
    }
}
