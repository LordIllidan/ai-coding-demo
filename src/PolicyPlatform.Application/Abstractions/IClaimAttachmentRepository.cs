using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Application.Abstractions;

public interface IClaimAttachmentRepository
{
    Task<ClaimAttachment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ClaimAttachment>> ListByClaimAsync(Guid claimId, CancellationToken ct = default);
    Task AddAsync(ClaimAttachment attachment, CancellationToken ct = default);
}
