using HelixScheduler.Infrastructure.Diagnostics;
using HelixScheduler.Infrastructure.Persistence.Seed;
using HelixScheduler.Infrastructure.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.Infrastructure.DependencyInjection;

internal static class InfrastructureHostSupportServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerInfrastructureHostSupport(
        this IServiceCollection services)
    {
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<ITenantBootstrapper, TenantBootstrapper>();
        services.AddScoped<IDemoSeedInitializer, DemoSeedInitializer>();
        services.AddScoped<IDiagnosticsService, DiagnosticsService>();
        services.AddScoped<IDemoScenarioStore, DemoScenarioStore>();
        services.AddScoped<IDemoSeedService, DemoSeedService>();
        services.AddScoped<IStartupInitializer, StartupInitializer>();

        return services;
    }
}
