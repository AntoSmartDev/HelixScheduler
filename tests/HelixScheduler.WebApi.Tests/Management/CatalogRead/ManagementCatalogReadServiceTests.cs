using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability.QueryServices;
using HelixScheduler.Application.Management.CatalogRead;
using HelixScheduler.Application.Management.Hierarchy;
using HelixScheduler.Application.Management.Validation;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Application.Management.ResourceCatalog;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Entities;
using HelixScheduler.Infrastructure.Persistence.QueryServices;
using HelixScheduler.Infrastructure.Persistence.Stores;
using HelixScheduler.Infrastructure.Persistence.Stores.Management.Validation;
using HelixScheduler.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class ManagementCatalogReadServiceTests
{
    [Fact]
    public async Task SchedulerCatalogSnapshot_Reuses_Active_Read_Side_And_Validation()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedSchedulerCatalogAsync(provider);

        var result = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IManagementCatalogReadService>();
            return await service.GetSchedulerCatalogSnapshotAsync(CancellationToken.None);
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(tenant.Id, result.Value!.Tenant.Id);
        Assert.Single(result.Value.ResourceTypes);
        Assert.Equal(1, result.Value.ResourceTypes[0].Id);
        Assert.Equal(new[] { 1, 2 }, result.Value.Resources.Select(resource => resource.Id).OrderBy(id => id).ToArray());
        Assert.Single(result.Value.HierarchyRelations);
        Assert.Equal(1, result.Value.HierarchyRelations[0].ParentResourceId);
        Assert.Equal(2, result.Value.HierarchyRelations[0].ChildResourceId);
        Assert.Single(result.Value.PropertySchema.Definitions);
        Assert.Equal(100, result.Value.PropertySchema.Definitions[0].Id);
        Assert.True(result.Value.Validation.IsValid);
    }

    [Fact]
    public async Task ResourceConfigurationSnapshot_Composes_Management_Query_And_Validation_Sides()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedResourceTroubleshootingScenarioAsync(provider);

        var result = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IManagementCatalogReadService>();
            return await service.GetResourceConfigurationSnapshotAsync(
                new ResourceConfigurationSnapshotRequest(
                    10,
                    new DateOnly(2026, 3, 1),
                    new DateOnly(2026, 3, 31)),
                CancellationToken.None);
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(10, result.Value!.Resource.Id);
        Assert.False(result.Value.Resource.IsActive);
        Assert.True(result.Value.Resource.IsArchived);
        Assert.NotNull(result.Value.ResourceType);
        Assert.Equal(1, result.Value.ResourceType!.Id);
        Assert.Single(result.Value.AssignedProperties);
        Assert.Equal(100, result.Value.AssignedProperties[0].Id);
        Assert.Single(result.Value.HierarchyRelations);
        Assert.Single(result.Value.Rules);
        Assert.Equal(500L, result.Value.Rules[0].Id);
        Assert.Single(result.Value.BusyEvents);
        Assert.Equal(600L, result.Value.BusyEvents[0].Id);
        Assert.False(result.Value.Validation.IsValid);
        Assert.Contains(result.Value.Validation.Findings, finding => finding.Code == "validation.resource.active-rule-on-inactive-resource");
        Assert.Contains(result.Value.Validation.Findings, finding => finding.Code == "validation.resource.active-busy-on-inactive-resource");
    }

    [Fact]
    public async Task Snapshot_Operations_Require_Tenant_Context()
    {
        await using var provider = BuildServiceProvider();

        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IManagementCatalogReadService>();

        var schedulerResult = await service.GetSchedulerCatalogSnapshotAsync(CancellationToken.None);
        var resourceResult = await service.GetResourceConfigurationSnapshotAsync(
            new ResourceConfigurationSnapshotRequest(
                1,
                new DateOnly(2026, 3, 1),
                new DateOnly(2026, 3, 31)),
            CancellationToken.None);

        Assert.False(schedulerResult.Succeeded);
        Assert.Equal("tenant.context.unresolved", schedulerResult.Errors[0].Code);
        Assert.False(resourceResult.Succeeded);
        Assert.Equal("tenant.context.unresolved", resourceResult.Errors[0].Code);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = $"management-catalog-read-{Guid.NewGuid()}";

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddSingleton<IClock>(new FixedClock(new DateTime(2026, 3, 21, 12, 0, 0, DateTimeKind.Utc)));
        services.AddDbContext<SchedulerDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<IResourceCatalogQueryService, ResourceCatalogQueryService>();
        services.AddScoped<IResourceTypeCatalogQueryService, ResourceTypeCatalogQueryService>();
        services.AddScoped<IPropertySchemaQueryService, PropertySchemaQueryService>();
        services.AddScoped<IAvailabilitySummaryQueryService, AvailabilitySummaryQueryService>();
        services.AddScoped<IResourceCatalogService, ResourceCatalogService>();
        services.AddScoped<IResourceTypeCatalogService, ResourceTypeCatalogService>();
        services.AddScoped<IPropertySchemaService, PropertySchemaService>();
        services.AddScoped<IResourceManagementStore, ResourceManagementStore>();
        services.AddScoped<IResourceTypeManagementStore, ResourceTypeManagementStore>();
        services.AddScoped<IHierarchyManagementStore, HierarchyManagementStore>();
        services.AddScoped<IManagementValidationStore, ManagementValidationStore>();
        services.AddScoped<IResourceManagementService, ResourceManagementService>();
        services.AddScoped<IResourceTypeManagementService, ResourceTypeManagementService>();
        services.AddScoped<IHierarchyManagementService, HierarchyManagementService>();
        services.AddScoped<IManagementValidationService, ManagementValidationService>();
        services.AddScoped<IManagementCatalogReadService, ManagementCatalogReadService>();

        return services.BuildServiceProvider();
    }

    private static async Task<TenantInfo> SeedSchedulerCatalogAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var tenant = await tenantStore.EnsureDefaultAsync(CancellationToken.None);
        tenantContext.SetTenant(tenant.Id, tenant.Key);

        dbContext.ResourceTypes.AddRange(
            new ResourceTypes { Id = 1, TenantId = tenant.Id, Key = "room", Label = "Room", SortOrder = 1, IsActive = true },
            new ResourceTypes { Id = 2, TenantId = tenant.Id, Key = "legacy", Label = "Legacy", SortOrder = 2, IsActive = false });

        dbContext.Resources.AddRange(
            new Resources
            {
                Id = 1,
                TenantId = tenant.Id,
                Code = "R1",
                Name = "Room 1",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 12, 0, 0, DateTimeKind.Utc)
            },
            new Resources
            {
                Id = 2,
                TenantId = tenant.Id,
                Code = "R2",
                Name = "Room 2",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 12, 5, 0, DateTimeKind.Utc)
            },
            new Resources
            {
                Id = 3,
                TenantId = tenant.Id,
                Code = "R3",
                Name = "Legacy Room",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                IsActive = false,
                IsArchived = true,
                CreatedAtUtc = new DateTime(2026, 3, 21, 12, 10, 0, DateTimeKind.Utc)
            });

        dbContext.ResourceRelations.Add(new ResourceRelations
        {
            TenantId = tenant.Id,
            ParentResourceId = 1,
            ChildResourceId = 2,
            RelationType = "Contains"
        });

        dbContext.ResourceProperties.AddRange(
            new ResourceProperties { Id = 100, TenantId = tenant.Id, Key = "Capability", Label = "Capability", ParentId = null, SortOrder = 1, IsActive = true },
            new ResourceProperties { Id = 200, TenantId = tenant.Id, Key = "Legacy", Label = "Legacy", ParentId = null, SortOrder = 2, IsActive = false });

        dbContext.ResourceTypeProperties.Add(new ResourceTypeProperties
        {
            TenantId = tenant.Id,
            ResourceTypeId = 1,
            PropertyDefinitionId = 100
        });

        dbContext.ResourcePropertyLinks.AddRange(
            new ResourcePropertyLinks { TenantId = tenant.Id, ResourceId = 1, PropertyId = 100 });

        await dbContext.SaveChangesAsync(CancellationToken.None);
        return tenant;
    }

    private static async Task<TenantInfo> SeedResourceTroubleshootingScenarioAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var tenant = await tenantStore.EnsureDefaultAsync(CancellationToken.None);
        tenantContext.SetTenant(tenant.Id, tenant.Key);

        dbContext.ResourceTypes.Add(new ResourceTypes
        {
            Id = 1,
            TenantId = tenant.Id,
            Key = "room",
            Label = "Room",
            SortOrder = 1,
            IsActive = true
        });

        dbContext.Resources.AddRange(
            new Resources
            {
                Id = 1,
                TenantId = tenant.Id,
                Code = "R1",
                Name = "Room 1",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 12, 0, 0, DateTimeKind.Utc)
            },
            new Resources
            {
                Id = 10,
                TenantId = tenant.Id,
                Code = "R10",
                Name = "Room 10",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                IsActive = false,
                IsArchived = true,
                CreatedAtUtc = new DateTime(2026, 3, 21, 12, 10, 0, DateTimeKind.Utc)
            });

        dbContext.ResourceRelations.Add(new ResourceRelations
        {
            TenantId = tenant.Id,
            ParentResourceId = 1,
            ChildResourceId = 10,
            RelationType = "Contains"
        });

        dbContext.ResourceProperties.Add(new ResourceProperties
        {
            Id = 100,
            TenantId = tenant.Id,
            Key = "Capability",
            Label = "Capability",
            ParentId = null,
            SortOrder = 1,
            IsActive = true
        });

        dbContext.ResourceTypeProperties.Add(new ResourceTypeProperties
        {
            TenantId = tenant.Id,
            ResourceTypeId = 1,
            PropertyDefinitionId = 100
        });

        dbContext.ResourcePropertyLinks.Add(new ResourcePropertyLinks
        {
            TenantId = tenant.Id,
            ResourceId = 10,
            PropertyId = 100
        });

        dbContext.Rules.AddRange(
            new Rules
            {
                Id = 500,
                TenantId = tenant.Id,
                Title = "Active Rule",
                Kind = (byte)HelixScheduler.Core.RuleKind.SingleDate,
                IsExclude = false,
                SingleDateUtc = new DateOnly(2026, 3, 25),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                IsActive = true,
                CreatedAtUtc = new DateTime(2026, 3, 21, 12, 0, 0, DateTimeKind.Utc)
            },
            new Rules
            {
                Id = 501,
                TenantId = tenant.Id,
                Title = "Inactive Rule",
                Kind = (byte)HelixScheduler.Core.RuleKind.SingleDate,
                IsExclude = false,
                SingleDateUtc = new DateOnly(2026, 3, 26),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                IsActive = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 12, 5, 0, DateTimeKind.Utc)
            });

        dbContext.RuleResources.AddRange(
            new RuleResources { TenantId = tenant.Id, RuleId = 500, ResourceId = 10 },
            new RuleResources { TenantId = tenant.Id, RuleId = 501, ResourceId = 10 });

        dbContext.BusyEvents.AddRange(
            new BusyEvents
            {
                Id = 600,
                TenantId = tenant.Id,
                Title = "Active Busy",
                StartUtc = new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2026, 3, 25, 13, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                CreatedAtUtc = new DateTime(2026, 3, 21, 12, 0, 0, DateTimeKind.Utc)
            },
            new BusyEvents
            {
                Id = 601,
                TenantId = tenant.Id,
                Title = "Inactive Busy",
                StartUtc = new DateTime(2026, 3, 25, 14, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2026, 3, 25, 15, 0, 0, DateTimeKind.Utc),
                IsActive = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 12, 5, 0, DateTimeKind.Utc)
            });

        dbContext.BusyEventResources.AddRange(
            new BusyEventResources { TenantId = tenant.Id, BusyEventId = 600, ResourceId = 10 },
            new BusyEventResources { TenantId = tenant.Id, BusyEventId = 601, ResourceId = 10 });

        await dbContext.SaveChangesAsync(CancellationToken.None);
        return tenant;
    }

    private static async Task<T> ExecuteForTenantAsync<T>(
        ServiceProvider provider,
        TenantInfo tenant,
        Func<IServiceProvider, Task<T>> action)
    {
        using var scope = provider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(tenant.Id, tenant.Key);
        return await action(scope.ServiceProvider);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
