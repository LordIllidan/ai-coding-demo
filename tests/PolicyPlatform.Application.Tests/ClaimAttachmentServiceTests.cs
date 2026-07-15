using PolicyPlatform.Application.Claims;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class ClaimAttachmentServiceTests
{
    private static readonly byte[] SampleContent = { 1, 2, 3, 4 };

    private static ClaimAttachmentService CreateService() => new(new InMemoryClaimAttachmentRepository());

    [Fact]
    public async Task UploadAsync_ValidFile_ReturnsDtoWithGeneratedId()
    {
        var service = CreateService();
        var claimId = Guid.NewGuid();

        var dto = await service.UploadAsync(claimId, "photo.jpg", "image/jpeg", SampleContent);

        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal(claimId, dto.ClaimId);
        Assert.Equal("photo.jpg", dto.FileName);
        Assert.Equal("image/jpeg", dto.ContentType);
        Assert.Equal(SampleContent.LongLength, dto.SizeBytes);
    }

    [Fact]
    public async Task UploadAsync_InvalidContentType_ThrowsDomainException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<DomainException>(
            () => service.UploadAsync(Guid.NewGuid(), "malware.exe", "application/octet-stream", SampleContent));
    }

    [Fact]
    public async Task UploadAsync_PersistsAttachment_VisibleViaListByClaim()
    {
        var service = CreateService();
        var claimId = Guid.NewGuid();

        var uploaded = await service.UploadAsync(claimId, "photo.jpg", "image/jpeg", SampleContent);
        var list = await service.ListByClaimAsync(claimId);

        Assert.Single(list);
        Assert.Equal(uploaded.Id, list[0].Id);
    }

    [Fact]
    public async Task ListByClaimAsync_UnknownClaim_ReturnsEmpty()
    {
        var service = CreateService();

        var list = await service.ListByClaimAsync(Guid.NewGuid());

        Assert.Empty(list);
    }

    [Fact]
    public async Task ListByClaimAsync_OnlyReturnsAttachmentsForRequestedClaim()
    {
        var service = CreateService();
        var claimA = Guid.NewGuid();
        var claimB = Guid.NewGuid();
        await service.UploadAsync(claimA, "a.jpg", "image/jpeg", SampleContent);
        await service.UploadAsync(claimB, "b.png", "image/png", SampleContent);

        var listA = await service.ListByClaimAsync(claimA);

        Assert.Single(listA);
        Assert.Equal("a.jpg", listA[0].FileName);
    }
}
