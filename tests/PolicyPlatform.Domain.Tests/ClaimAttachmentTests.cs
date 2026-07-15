using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Common;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class ClaimAttachmentTests
{
    private static readonly byte[] SampleContent = { 1, 2, 3, 4 };

    private static ClaimAttachment CreateAttachment(
        Guid? claimId = null,
        string fileName = "photo.jpg",
        string contentType = "image/jpeg",
        byte[]? content = null,
        DateTimeOffset? uploadedAtUtc = null)
        => ClaimAttachment.Create(
            Guid.NewGuid(),
            claimId ?? Guid.NewGuid(),
            fileName,
            contentType,
            content ?? SampleContent,
            uploadedAtUtc ?? DateTimeOffset.UtcNow);

    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var uploadedAt = DateTimeOffset.UtcNow;

        var attachment = ClaimAttachment.Create(id, claimId, "photo.jpg", "image/jpeg", SampleContent, uploadedAt);

        Assert.Equal(id, attachment.Id);
        Assert.Equal(claimId, attachment.ClaimId);
        Assert.Equal("photo.jpg", attachment.FileName);
        Assert.Equal("image/jpeg", attachment.ContentType);
        Assert.Equal(SampleContent, attachment.Content);
        Assert.Equal(SampleContent.LongLength, attachment.SizeBytes);
        Assert.Equal(uploadedAt, attachment.UploadedAtUtc);
    }

    [Fact]
    public void Create_EmptyClaimId_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => CreateAttachment(claimId: Guid.Empty));
        Assert.Contains("claim", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingFileName_Throws(string fileName)
    {
        var ex = Assert.Throws<DomainException>(() => CreateAttachment(fileName: fileName));
        Assert.Contains("file name", ex.Message);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("")]
    public void Create_DisallowedContentType_Throws(string contentType)
    {
        Assert.Throws<DomainException>(() => CreateAttachment(contentType: contentType));
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("IMAGE/JPEG")]
    [InlineData("image/png")]
    [InlineData("image/heic")]
    [InlineData("image/webp")]
    public void Create_AllowedContentType_CaseInsensitive_Succeeds(string contentType)
    {
        var attachment = CreateAttachment(contentType: contentType);

        Assert.Equal(contentType, attachment.ContentType);
    }

    [Fact]
    public void Create_EmptyContent_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => CreateAttachment(content: Array.Empty<byte>()));
        Assert.Contains("empty", ex.Message);
    }

    [Fact]
    public void Create_ContentExceedsMaxSize_Throws()
    {
        var oversized = new byte[15 * 1024 * 1024 + 1];

        var ex = Assert.Throws<DomainException>(() => CreateAttachment(content: oversized));
        Assert.Contains("maximum allowed size", ex.Message);
    }

    [Fact]
    public void Create_ContentAtMaxSize_Succeeds()
    {
        var maxSized = new byte[15 * 1024 * 1024];

        var attachment = CreateAttachment(content: maxSized);

        Assert.Equal(maxSized.LongLength, attachment.SizeBytes);
    }
}
