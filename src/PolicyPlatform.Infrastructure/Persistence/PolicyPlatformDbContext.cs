using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PolicyPlatformDbContext).Assembly);
    }
}
