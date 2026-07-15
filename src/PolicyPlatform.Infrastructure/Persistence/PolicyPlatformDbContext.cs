using Microsoft.EntityFrameworkCore;
using PolicyPlatform.Domain.Claims;

namespace PolicyPlatform.Infrastructure.Persistence;

public sealed class PolicyPlatformDbContext : DbContext
{
    public PolicyPlatformDbContext(DbContextOptions<PolicyPlatformDbContext> options) : base(options)
    {
    }

    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PolicyPlatformDbContext).Assembly);
    }
}
