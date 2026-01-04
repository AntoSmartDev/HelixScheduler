using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Demo;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Core;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerWebApi(this IServiceCollection services)
    {
        services.AddSingleton<AvailabilityEngineV1>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IDemoReadService, DemoReadService>();
        services.AddScoped<IResourceCatalogService, ResourceCatalogService>();
        services.AddScoped<IResourceTypeCatalogService, ResourceTypeCatalogService>();
        services.AddScoped<IPropertySchemaService, PropertySchemaService>();
        return services;
    }
}
