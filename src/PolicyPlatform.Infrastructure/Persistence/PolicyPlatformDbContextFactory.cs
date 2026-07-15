using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PolicyPlatform.Infrastructure.Persistence;

/// <summary>Used only by `dotnet ef` CLI tooling at design time (migrations add/update) —
/// reads the connection string from the CONNECTIONSTRINGS__POLICYPLATFORMDB env var directly,
/// bypassing the app's normal hosting/DI pipeline which the EF CLI does not reliably invoke
/// for minimal-API top-level Program.cs.</summary>
public sealed class PolicyPlatformDbContextFactory : IDesignTimeDbContextFactory<PolicyPlatformDbContext>
{
    public PolicyPlatformDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PolicyPlatformDb")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__PolicyPlatformDb before running dotnet ef commands.");

        var options = new DbContextOptionsBuilder<PolicyPlatformDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new PolicyPlatformDbContext(options);
    }
}
