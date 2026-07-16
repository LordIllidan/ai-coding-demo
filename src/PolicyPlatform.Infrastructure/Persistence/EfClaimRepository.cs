using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>EF Core-backed <see cref="IClaimRepository"/>, used when a SQL connection string is
/// configured (see <see cref="PolicyPlatform.Infrastructure.DependencyInjection.AddPolicyPlatformInfrastructure"/>).</summary>
public sealed class EfClaimRepository : IClaimRepository
{
    private readonly PolicyPlatformDbContext _db;

    public EfClaimRepository(PolicyPlatformDbContext db) => _db = db;

    /// <summary>Retrieves a theft claim by id.</summary>
    /// <param name="id">Identifier of the claim.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The claim, or <c>null</c> if none exists with that id.</returns>
    public async Task<TheftClaim?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.TheftClaims.FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <summary>Persists a new theft claim and saves changes immediately.</summary>
    /// <param name="claim">The claim to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AddAsync(TheftClaim claim, CancellationToken ct = default)
    {
        await _db.TheftClaims.AddAsync(claim, ct);
        await _db.SaveChangesAsync(ct);
    }
}
