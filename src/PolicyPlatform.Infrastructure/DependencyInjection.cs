using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Customers;
using PolicyPlatform.Application.Notifications;
using PolicyPlatform.Application.Policies;
using PolicyPlatform.Infrastructure.Notifications;
using PolicyPlatform.Infrastructure.Numbering;
using PolicyPlatform.Infrastructure.Persistence;

namespace PolicyPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPolicyPlatformInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
        services.AddSingleton<IDeviceTokenRepository, InMemoryDeviceTokenRepository>();
        services.AddSingleton<IPushNotificationSender, LoggingPushNotificationSender>();
        services.Configure<PushNotificationOptions>(
            configuration.GetSection(PushNotificationOptions.SectionName));
        services.AddScoped<PolicyStatusPushDispatcher>();
        services.AddScoped<DeviceRegistrationService>();
        services.AddScoped<PolicyService>();
        services.AddScoped<CustomerService>();
        return services;
    }
}
