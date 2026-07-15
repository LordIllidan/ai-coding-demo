using Microsoft.Extensions.DependencyInjection;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Application.Customers;
using PolicyPlatform.Application.Policies;
using PolicyPlatform.Infrastructure.Numbering;
using PolicyPlatform.Infrastructure.Persistence;

namespace PolicyPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPolicyPlatformInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddSingleton<IClaimAttachmentRepository, InMemoryClaimAttachmentRepository>();
        services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
        services.AddScoped<PolicyService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<ClaimAttachmentService>();
        return services;
    }
}
