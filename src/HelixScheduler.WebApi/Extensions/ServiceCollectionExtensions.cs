using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability.Orchestration;
using HelixScheduler.Application.BusyEventManagement;
using HelixScheduler.Application.CatalogRead;
using HelixScheduler.Application.Demo;
using HelixScheduler.Application.Hierarchy;
using HelixScheduler.Application.ManagementValidation;
using HelixScheduler.Application.PropertyManagement;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Application.ResourceCatalog.Management;
using HelixScheduler.Application.RuleManagement;
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
        services.AddScoped<IBusyEventManagementService, BusyEventManagementService>();
        services.AddScoped<IManagementCatalogReadService, ManagementCatalogReadService>();
        services.AddScoped<IDemoReadService, DemoReadService>();
        services.AddScoped<IHierarchyManagementService, HierarchyManagementService>();
        services.AddScoped<IManagementValidationService, ManagementValidationService>();
        services.AddScoped<IPropertyManagementService, PropertyManagementService>();
        services.AddScoped<IResourcePropertyAssignmentManagementService, ResourcePropertyAssignmentManagementService>();
        services.AddScoped<IResourceCatalogService, ResourceCatalogService>();
        services.AddScoped<IResourceTypeCatalogService, ResourceTypeCatalogService>();
        services.AddScoped<IResourceTypeManagementService, ResourceTypeManagementService>();
        services.AddScoped<IResourceManagementService, ResourceManagementService>();
        services.AddScoped<IRuleManagementService, RuleManagementService>();
        services.AddScoped<IPropertySchemaService, PropertySchemaService>();
        services.AddScoped<ITenantManagementService, TenantManagementService>();
        return services;
    }
}


