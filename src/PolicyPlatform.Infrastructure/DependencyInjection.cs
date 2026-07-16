using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Application.Customers;
using PolicyPlatform.Application.Policies;
using PolicyPlatform.Application.Sms;
using PolicyPlatform.Infrastructure.Numbering;
using PolicyPlatform.Infrastructure.Persistence;
using PolicyPlatform.Infrastructure.Sms;

namespace PolicyPlatform.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Wires persistence: SQL Server (EF Core) when a "PolicyPlatformDb" connection
    /// string is configured (e.g. Azure App Service Connection Strings, or local
    /// appsettings/user-secrets), otherwise falls back to the in-memory repositories so the
    /// app still runs with zero external dependencies for local dev/demo.</summary>
    public static IServiceCollection AddPolicyPlatformInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PolicyPlatformDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
            services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        }
        else
        {
            services.AddDbContext<PolicyPlatformDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<IPolicyRepository, EfPolicyRepository>();
            services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        }

        services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
        services.AddScoped<PolicyService>();
        services.AddScoped<CustomerService>();

        // Claims have no durable store yet (EF Core provider is a separate, unscoped
        // piece of work) — in-memory keeps the theft-claim validation flow runnable now.
        services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
        services.AddScoped<ClaimService>();

        // SMS policy-status decision logic (policy/PESEL lookup) is a separate, unscoped
        // piece of work — the placeholder keeps the request/validation flow runnable now.
        services.AddSingleton<IPolicyStatusRequestHandler, PendingPolicyStatusRequestHandler>();
        return services;
    }
}
