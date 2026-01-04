using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Demo;
using HelixScheduler.Application.Diagnostics;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Application.Startup;
using HelixScheduler.Infrastructure.Diagnostics;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Repositories;
using HelixScheduler.Infrastructure.Persistence.Seed;
using HelixScheduler.Infrastructure.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerInfrastructure(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        var provider = cfg["HelixScheduler:DatabaseProvider"] ?? "Sqlite";
        services.AddDbContext<SchedulerDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = cfg.GetConnectionString("SchedulerDb");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("ConnectionStrings:SchedulerDb is required for SqlServer.");
                }

                options.UseSqlServer(connectionString);
            }
            else
            {
                throw new InvalidOperationException(
                    "SQLite support is temporarily disabled. Set HelixScheduler:DatabaseProvider to SqlServer.");
            }
        });

        services.AddScoped<IRuleRepository, RuleRepository>();
        services.AddScoped<IBusyEventRepository, BusyEventRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<IAvailabilityDataSource, AvailabilityDataSource>();
        services.AddScoped<IResourceCatalogDataSource, ResourceCatalogDataSource>();
        services.AddScoped<IResourceTypeCatalogDataSource, ResourceTypeCatalogDataSource>();
        services.AddScoped<IPropertySchemaDataSource, PropertySchemaDataSource>();
        services.AddScoped<IDiagnosticsService, DiagnosticsService>();
        services.AddScoped<IDemoScenarioStore, DemoScenarioStore>();
        services.AddScoped<IDemoSeedService, DemoSeedService>();
        services.AddScoped<IStartupInitializer, StartupInitializer>();

        return services;
    }
}


