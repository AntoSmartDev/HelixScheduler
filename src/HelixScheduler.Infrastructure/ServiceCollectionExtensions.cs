using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability.Orchestration;
using HelixScheduler.Application.Availability.QueryServices;
using HelixScheduler.Application.Demo;
using HelixScheduler.Application.Diagnostics;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Application.ResourceCatalog.Management;
using HelixScheduler.Application.Hierarchy;
using HelixScheduler.Application.Tenancy;
using HelixScheduler.Application.Startup;
using HelixScheduler.Infrastructure.Diagnostics;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.QueryServices;
using HelixScheduler.Infrastructure.Persistence.Stores;
using HelixScheduler.Infrastructure.Persistence.Seed;
using HelixScheduler.Infrastructure.Startup;
using HelixScheduler.Infrastructure.Tenancy;
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
        var connectionString = cfg.GetConnectionString("SchedulerDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:SchedulerDb is required.");
        }

        services.AddDbContext<SchedulerDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<ITenantManagementStore, TenantManagementStore>();
        services.AddScoped<IHierarchyManagementStore, HierarchyManagementStore>();
        services.AddScoped<IResourceTypeManagementStore, ResourceTypeManagementStore>();
        services.AddScoped<IResourceManagementStore, ResourceManagementStore>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<ITenantBootstrapper, TenantBootstrapper>();
        services.AddScoped<IDemoSeedInitializer, DemoSeedInitializer>();
        services.AddScoped<PropertyHierarchyQueryService>();
        services.AddScoped<IAvailabilityComputeQueryService, AvailabilityComputeQueryService>();
        services.AddScoped<IAvailabilityFilterQueryService, AvailabilityFilterQueryService>();
        services.AddScoped<IAvailabilityAncestorQueryService, AvailabilityAncestorQueryService>();
        services.AddScoped<IAvailabilitySummaryQueryService, AvailabilitySummaryQueryService>();
        services.AddScoped<IResourceCatalogQueryService, ResourceCatalogQueryService>();
        services.AddScoped<IResourceTypeCatalogQueryService, ResourceTypeCatalogQueryService>();
        services.AddScoped<IPropertySchemaQueryService, PropertySchemaQueryService>();
        services.AddScoped<IDiagnosticsService, DiagnosticsService>();
        services.AddScoped<IDemoScenarioStore, DemoScenarioStore>();
        services.AddScoped<IDemoSeedService, DemoSeedService>();
        services.AddScoped<IStartupInitializer, StartupInitializer>();

        return services;
    }
}


