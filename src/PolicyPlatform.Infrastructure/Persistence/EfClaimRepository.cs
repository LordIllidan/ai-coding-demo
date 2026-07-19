using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>SQL Server-backed <see cref="IClaimRepository"/> implementation.</summary>
public sealed class EfClaimRepository : IClaimRepository
{
    private readonly PolicyPlatformDbContext _db;

    public EfClaimRepository(PolicyPlatformDbContext db) => _db = db;

    /// <summary>Fetches a theft claim by id.</summary>
    /// <param name="id">Claim id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The claim, or <see langword="null"/> if none exists with that id.</returns>
    public async Task<TheftClaim?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.TheftClaims.FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <summary>Persists a newly registered theft claim.</summary>
    /// <param name="claim">Claim to insert.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AddAsync(TheftClaim claim, CancellationToken ct = default)
    {
        await _db.TheftClaims.AddAsync(claim, ct);
        await _db.SaveChangesAsync(ct);
    }
}
