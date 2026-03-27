using HelixScheduler.Infrastructure;
using HelixScheduler.Infrastructure.Startup;
using HelixScheduler.Infrastructure.SqlServer.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.Infrastructure.SqlServer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerInfrastructureSqlServer(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        var connectionString = cfg.GetConnectionString("SchedulerDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:SchedulerDb is required.");
        }

        services
            .AddHelixSchedulerInfrastructureCommon()
            .AddHelixSchedulerInfrastructureSqlServerProvider(connectionString)
            .AddScoped<IStartupInitializer, StartupInitializer>();

        return services;
    }
}
