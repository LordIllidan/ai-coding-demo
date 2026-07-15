using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Claims;

/// <summary>A photo (camera or gallery) attached to a damage claim submitted from the
/// mobile app. Backend counterpart of AISDLC-10 — validates what the mobile picker
/// uploads; thumbnails and OS-level permission handling stay client-side.</summary>
public sealed class ClaimAttachment : Entity
{
    private static readonly IReadOnlyCollection<string> AllowedContentTypes =
        new[] { "image/jpeg", "image/png", "image/heic", "image/webp" };

    private const long MaxSizeBytes = 15 * 1024 * 1024; // 15 MB

    public Guid ClaimId { get; }
    public string FileName { get; }
    public string ContentType { get; }
    public long SizeBytes { get; }
    public byte[] Content { get; }
    public DateTimeOffset UploadedAtUtc { get; }

    private ClaimAttachment(
        Guid id, Guid claimId, string fileName, string contentType, byte[] content, DateTimeOffset uploadedAtUtc)
        : base(id)
    {
        ClaimId = claimId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = content.LongLength;
        Content = content;
        UploadedAtUtc = uploadedAtUtc;
    }

    public static ClaimAttachment Create(
        Guid id, Guid claimId, string fileName, string contentType, byte[] content, DateTimeOffset uploadedAtUtc)
    {
        if (claimId == Guid.Empty)
        {
            throw new DomainException("Attachment must belong to a valid claim.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("Attachment file name is required.");
        }

        if (!AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainException(
                $"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}.");
        }

        if (content.LongLength <= 0)
        {
            throw new DomainException("Attachment file is empty.");
        }

        if (content.LongLength > MaxSizeBytes)
        {
            throw new DomainException($"Attachment exceeds the maximum allowed size of {MaxSizeBytes / (1024 * 1024)} MB.");
        }

        return new ClaimAttachment(id, claimId, fileName, contentType, content, uploadedAtUtc);
    }
}
