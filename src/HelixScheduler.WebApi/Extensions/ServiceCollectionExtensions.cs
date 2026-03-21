using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability.Orchestration;
using HelixScheduler.Application.Demo;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Application.Tenancy;
using HelixScheduler.Core;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerWebApi(this IServiceCollection services)
    {
        services.AddSingleton<AvailabilityEngine>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IDemoReadService, DemoReadService>();
        services.AddScoped<IResourceCatalogService, ResourceCatalogService>();
        services.AddScoped<IResourceTypeCatalogService, ResourceTypeCatalogService>();
        services.AddScoped<IPropertySchemaService, PropertySchemaService>();
        services.AddScoped<ITenantManagementService, TenantManagementService>();
        return services;
    }
}


