using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>SQL Server-backed <see cref="IClaimPayoutRepository"/>, used when a
/// "PolicyPlatformDb" connection string is configured.</summary>
public sealed class EfClaimPayoutRepository : IClaimPayoutRepository
{
    private readonly PolicyPlatformDbContext _db;

    public EfClaimPayoutRepository(PolicyPlatformDbContext db) => _db = db;

    /// <inheritdoc/>
    public async Task<ClaimPayout?> GetLastPaidPayoutAsync(Guid customerId, CancellationToken ct = default)
        => await _db.ClaimPayouts
            .Where(p => p.CustomerId == customerId && p.Status == ClaimPayoutStatus.Paid)
            .OrderByDescending(p => p.PaidAt)
            .FirstOrDefaultAsync(ct);
}
