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
    public static IServiceCollection AddPolicyPlatformInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
        services.AddSingleton<IDeviceRegistrationRepository, InMemoryDeviceRegistrationRepository>();
        services.AddSingleton<IPushNotificationSender, LoggingPushNotificationSender>();
        services.AddScoped<PolicyService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<DeviceRegistrationService>();
        services.AddScoped<PolicyStatusNotificationService>();
        return services;
    }
}
