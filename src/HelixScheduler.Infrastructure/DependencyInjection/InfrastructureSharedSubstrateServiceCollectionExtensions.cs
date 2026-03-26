using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability.QueryServices;
using HelixScheduler.Application.Management.BusyEvents;
using HelixScheduler.Application.Management.Hierarchy;
using HelixScheduler.Application.Management.Properties;
using HelixScheduler.Application.Management.ResourceCatalog;
using HelixScheduler.Application.Management.Rules;
using HelixScheduler.Application.Management.Tenants;
using HelixScheduler.Application.Management.Validation;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Infrastructure.Persistence.QueryServices;
using HelixScheduler.Infrastructure.Persistence.Stores;
using HelixScheduler.Infrastructure.Persistence.Stores.Management.BusyEvents;
using HelixScheduler.Infrastructure.Persistence.Stores.Management.Rules;
using HelixScheduler.Infrastructure.Persistence.Stores.Management.Tenants;
using HelixScheduler.Infrastructure.Persistence.Stores.Management.Validation;
using HelixScheduler.Infrastructure.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.Infrastructure.DependencyInjection;

internal static class InfrastructureSharedSubstrateServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerInfrastructureSharedSubstrate(
        this IServiceCollection services)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<ITenantManagementStore, TenantManagementStore>();
        services.AddScoped<IBusyEventManagementStore, BusyEventManagementStore>();
        services.AddScoped<IManagementValidationStore, ManagementValidationStore>();
        services.AddScoped<IHierarchyManagementStore, HierarchyManagementStore>();
        services.AddScoped<IPropertyManagementStore, PropertyManagementStore>();
        services.AddScoped<IResourcePropertyAssignmentManagementStore, ResourcePropertyAssignmentManagementStore>();
        services.AddScoped<IResourceTypeManagementStore, ResourceTypeManagementStore>();
        services.AddScoped<IResourceTypePropertySchemaManagementStore, ResourceTypePropertySchemaManagementStore>();
        services.AddScoped<IResourceManagementStore, ResourceManagementStore>();
        services.AddScoped<IRuleManagementStore, RuleManagementStore>();
        services.AddScoped<PropertyHierarchyQueryService>();
        services.AddScoped<IAvailabilityComputeQueryService, AvailabilityComputeQueryService>();
        services.AddScoped<IAvailabilityFilterQueryService, AvailabilityFilterQueryService>();
        services.AddScoped<IAvailabilityAncestorQueryService, AvailabilityAncestorQueryService>();
        services.AddScoped<IAvailabilitySummaryQueryService, AvailabilitySummaryQueryService>();
        services.AddScoped<IResourceCatalogQueryService, ResourceCatalogQueryService>();
        services.AddScoped<IResourceTypeCatalogQueryService, ResourceTypeCatalogQueryService>();
        services.AddScoped<IPropertySchemaQueryService, PropertySchemaQueryService>();

        return services;
    }
}
