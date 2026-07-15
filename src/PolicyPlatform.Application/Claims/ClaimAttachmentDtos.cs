using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Claims;

public sealed record ClaimAttachmentDto(
    Guid Id, Guid ClaimId, string FileName, string ContentType, long SizeBytes, DateTimeOffset UploadedAtUtc)
{
    public static ClaimAttachmentDto FromDomain(ClaimAttachment attachment) => new(
        attachment.Id,
        attachment.ClaimId,
        attachment.FileName,
        attachment.ContentType,
        attachment.SizeBytes,
        attachment.UploadedAtUtc);
}
