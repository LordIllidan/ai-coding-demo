using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

/// <summary>Use-case layer for uploading claim photos taken with the mobile camera or
/// picked from the gallery (AISDLC-10). Validation rules (allowed type/size) live on the
/// domain entity; this service only orchestrates persistence.</summary>
public sealed class ClaimAttachmentService
{
    private readonly IClaimAttachmentRepository _attachments;

    public ClaimAttachmentService(IClaimAttachmentRepository attachments) => _attachments = attachments;

    public async Task<ClaimAttachmentDto> UploadAsync(
        Guid claimId, string fileName, string contentType, byte[] content, CancellationToken ct = default)
    {
        var attachment = ClaimAttachment.Create(
            Guid.NewGuid(), claimId, fileName, contentType, content, DateTimeOffset.UtcNow);

        await _attachments.AddAsync(attachment, ct);
        return ClaimAttachmentDto.FromDomain(attachment);
    }

    public async Task<IReadOnlyList<ClaimAttachmentDto>> ListByClaimAsync(Guid claimId, CancellationToken ct = default)
    {
        var attachments = await _attachments.ListByClaimAsync(claimId, ct);
        return attachments.Select(ClaimAttachmentDto.FromDomain).ToList();
    }
}
