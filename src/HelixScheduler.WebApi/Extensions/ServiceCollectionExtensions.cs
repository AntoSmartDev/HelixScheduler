using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability.Orchestration;
using HelixScheduler.Application.Management.BusyEvents;
using HelixScheduler.Application.Management.CatalogRead;
using HelixScheduler.Application.Management.Hierarchy;
using HelixScheduler.Application.Management.Properties;
using HelixScheduler.Application.Management.ResourceCatalog;
using HelixScheduler.Application.Management.Rules;
using HelixScheduler.Application.Management.Tenants;
using HelixScheduler.Application.Management.Validation;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Core;
using HelixScheduler.WebApi.Demo;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerWebApi(this IServiceCollection services)
    {
        services.AddSingleton<AvailabilityEngine>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IBusyEventManagementService, BusyEventManagementService>();
        services.AddScoped<IManagementCatalogReadService, ManagementCatalogReadService>();
        services.AddScoped<IDemoReadService, DemoReadService>();
        services.AddScoped<IHierarchyManagementService, HierarchyManagementService>();
        services.AddScoped<ILegacyConsistencyService, LegacyConsistencyService>();
        services.AddScoped<IManagementValidationService, ManagementValidationService>();
        services.AddScoped<IPropertyManagementService, PropertyManagementService>();
        services.AddScoped<IResourcePropertyAssignmentManagementService, ResourcePropertyAssignmentManagementService>();
        services.AddScoped<IResourceCatalogService, ResourceCatalogService>();
        services.AddScoped<IResourceTypeCatalogService, ResourceTypeCatalogService>();
        services.AddScoped<IResourceTypeManagementService, ResourceTypeManagementService>();
        services.AddScoped<IResourceTypePropertySchemaManagementService, ResourceTypePropertySchemaManagementService>();
        services.AddScoped<IResourceManagementService, ResourceManagementService>();
        services.AddScoped<IRuleManagementService, RuleManagementService>();
        services.AddScoped<IPropertySchemaService, PropertySchemaService>();
        services.AddScoped<ITenantManagementService, TenantManagementService>();
        return services;
    }
}
