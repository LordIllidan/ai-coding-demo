using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class EfClaimPayoutRepository : IClaimPayoutRepository
{
    private readonly PolicyPlatformDbContext _db;

    public EfClaimPayoutRepository(PolicyPlatformDbContext db) => _db = db;

    public async Task<ClaimPayout?> GetLastPaidPayoutAsync(Guid customerId, CancellationToken ct = default)
        => await _db.ClaimPayouts
            .Where(p => p.CustomerId == customerId && p.Status == ClaimPayoutStatus.Paid)
            .OrderByDescending(p => p.PaidAt)
            .FirstOrDefaultAsync(ct);
}
