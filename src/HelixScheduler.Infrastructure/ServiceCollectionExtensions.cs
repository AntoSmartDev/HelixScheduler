using HelixScheduler.Infrastructure.DependencyInjection;
using HelixScheduler.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerInfrastructure(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        var connectionString = cfg.GetConnectionString("SchedulerDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:SchedulerDb is required.");
        }

        services
            .AddHelixSchedulerInfrastructureSqlServer(connectionString)
            .AddHelixSchedulerInfrastructureSharedSubstrate()
            .AddHelixSchedulerInfrastructureHostSupport();

        return services;
    }
}
