using System.Collections.Concurrent;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Process-lifetime in-memory store. Swap for blob storage once a real backend is
/// provisioned — the Application layer only depends on IClaimAttachmentRepository.</summary>
public sealed class InMemoryClaimAttachmentRepository : IClaimAttachmentRepository
{
    private readonly ConcurrentDictionary<Guid, ClaimAttachment> _attachments = new();

    public Task<ClaimAttachment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_attachments.GetValueOrDefault(id));

    public Task<IReadOnlyList<ClaimAttachment>> ListByClaimAsync(Guid claimId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ClaimAttachment>>(
            _attachments.Values.Where(a => a.ClaimId == claimId).ToList());

    public Task AddAsync(ClaimAttachment attachment, CancellationToken ct = default)
    {
        _attachments[attachment.Id] = attachment;
        return Task.CompletedTask;
    }
}
