using Microsoft.AspNetCore.Mvc;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Controllers;

/// <summary>Receives claim photos uploaded by the mobile app's camera/gallery picker
/// (AISDLC-10). File type and size are validated by the domain layer; this controller only
/// translates the multipart request into bytes and maps domain errors to 400 responses.</summary>
[ApiController]
[Route("api/claims/{claimId:guid}/attachments")]
public sealed class ClaimAttachmentsController : ControllerBase
{
    private readonly ClaimAttachmentService _claimAttachments;

    public ClaimAttachmentsController(ClaimAttachmentService claimAttachments) => _claimAttachments = claimAttachments;

    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ClaimAttachmentDto>> Upload(Guid claimId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Problem("No file was uploaded.", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);

            var attachment = await _claimAttachments.UploadAsync(
                claimId, file.FileName, file.ContentType, stream.ToArray(), ct);

            return CreatedAtAction(nameof(List), new { claimId }, attachment);
        }
        catch (DomainException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClaimAttachmentDto>>> List(Guid claimId, CancellationToken ct)
        => Ok(await _claimAttachments.ListByClaimAsync(claimId, ct));
}
