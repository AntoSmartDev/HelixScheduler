using HelixScheduler.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerInfrastructureCommon(
        this IServiceCollection services)
    {
        services
            .AddHelixSchedulerInfrastructureSharedSubstrate()
            .AddHelixSchedulerInfrastructureHostSupport();

        return services;
    }
}
