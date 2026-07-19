using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Customers;
using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class PolicyPlatformDbContext : DbContext
{
    public PolicyPlatformDbContext(DbContextOptions<PolicyPlatformDbContext> options) : base(options)
    {
    }

    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Customer> Customers => Set<Customer>();
    /// <summary>Vehicle theft claims (<c>theft_claim</c> table).</summary>
    public DbSet<TheftClaim> TheftClaims => Set<TheftClaim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PolicyPlatformDbContext).Assembly);
    }
}
