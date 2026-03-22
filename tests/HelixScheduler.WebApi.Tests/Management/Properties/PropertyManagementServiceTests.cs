using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;
using HelixScheduler.Application.Management.Properties;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.Management.ResourceCatalog;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Entities;
using HelixScheduler.Infrastructure.Persistence.QueryServices;
using HelixScheduler.Infrastructure.Persistence.Stores;
using HelixScheduler.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class PropertyManagementServiceTests
{
    [Fact]
    public async Task Create_Update_List_And_Activate_Deactivate_Property_Work()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedCatalogAsync(provider);

        var created = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.CreatePropertyAsync(
                new CreatePropertyCommand("FloorFeature", "Floor Feature", 7),
                CancellationToken.None);
        });

        Assert.True(created.Succeeded);
        Assert.True(created.Value!.IsActive);

        var updated = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.UpdatePropertyAsync(
                new UpdatePropertyCommand(created.Value!.Id, "FloorFeature", "Floor Feature Updated", 8),
                CancellationToken.None);
        });

        Assert.True(updated.Succeeded);
        Assert.Equal("Floor Feature Updated", updated.Value!.Label);

        var deactivated = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.DeactivatePropertyAsync(created.Value!.Id, CancellationToken.None);
        });

        Assert.True(deactivated.Succeeded);
        Assert.False(deactivated.Value!.IsActive);

        var activated = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.ActivatePropertyAsync(created.Value!.Id, CancellationToken.None);
        });

        Assert.True(activated.Succeeded);
        Assert.True(activated.Value!.IsActive);

        var listed = await ExecuteForTenantAsync(provider, tenant, sp =>
            sp.GetRequiredService<IPropertyManagementService>().ListPropertiesAsync(CancellationToken.None));

        Assert.Contains(listed, property => property.Id == created.Value!.Id);
    }

    [Fact]
    public async Task Property_Tree_Rejects_Duplicates_Cycles_And_Inactive_Properties()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedCatalogAsync(provider);

        var addOne = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.AddPropertyParentRelationAsync(
                new AddPropertyParentRelationCommand(103, 104),
                CancellationToken.None);
        });

        Assert.True(addOne.Succeeded);

        var duplicate = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.AddPropertyParentRelationAsync(
                new AddPropertyParentRelationCommand(103, 104),
                CancellationToken.None);
        });

        Assert.False(duplicate.Succeeded);
        Assert.Equal("property.relation.already-exists", duplicate.Errors[0].Code);

        await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.AddPropertyParentRelationAsync(
                new AddPropertyParentRelationCommand(104, 102),
                CancellationToken.None);
        });

        var cycle = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.AddPropertyParentRelationAsync(
                new AddPropertyParentRelationCommand(102, 103),
                CancellationToken.None);
        });

        Assert.False(cycle.Succeeded);
        Assert.Equal("property.tree.cycle-detected", cycle.Errors[0].Code);

        var created = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.CreatePropertyAsync(
                new CreatePropertyCommand("Transient", "Transient", null),
                CancellationToken.None);
        });

        Assert.True(created.Succeeded);

        await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.DeactivatePropertyAsync(created.Value!.Id, CancellationToken.None);
        });

        var inactiveParent = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.AddPropertyParentRelationAsync(
                new AddPropertyParentRelationCommand(created.Value!.Id, 101),
                CancellationToken.None);
        });

        Assert.False(inactiveParent.Succeeded);
        Assert.Equal("property.parent.inactive", inactiveParent.Errors[0].Code);
    }

    [Fact]
    public async Task Property_Deactivation_Is_Blocked_When_Still_Referenced()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedCatalogAsync(provider);

        var withChild = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.DeactivatePropertyAsync(100, CancellationToken.None);
        });

        Assert.False(withChild.Succeeded);
        Assert.Equal("property.lifecycle.child-properties-exist", withChild.Errors[0].Code);

        var withTypeMapping = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.DeactivatePropertyAsync(200, CancellationToken.None);
        });

        Assert.False(withTypeMapping.Succeeded);
        Assert.Equal("property.lifecycle.type-mappings-exist", withTypeMapping.Errors[0].Code);

        await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var assignmentService = sp.GetRequiredService<IResourcePropertyAssignmentManagementService>();
            return await assignmentService.AssignPropertiesToResourceAsync(
                new AssignPropertiesToResourceCommand(1, new[] { 301 }),
                CancellationToken.None);
        });

        var withAssignment = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IPropertyManagementService>();
            return await service.DeactivatePropertyAsync(301, CancellationToken.None);
        });

        Assert.False(withAssignment.Succeeded);
        Assert.Equal("property.lifecycle.resource-assignments-exist", withAssignment.Errors[0].Code);
    }

    [Fact]
    public async Task Assign_Remove_And_List_Properties_Respect_Type_Compatibility()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedCatalogAsync(provider);

        var assigned = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourcePropertyAssignmentManagementService>();
            return await service.AssignPropertiesToResourceAsync(
                new AssignPropertiesToResourceCommand(1, new[] { 301 }),
                CancellationToken.None);
        });

        Assert.True(assigned.Succeeded);
        Assert.Contains(assigned.Value!.Properties, property => property.Id == 301);

        var incompatible = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourcePropertyAssignmentManagementService>();
            return await service.AssignPropertiesToResourceAsync(
                new AssignPropertiesToResourceCommand(1, new[] { 200 }),
                CancellationToken.None);
        });

        Assert.False(incompatible.Succeeded);
        Assert.Equal("property.assignment.type-incompatibility", incompatible.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.Validation, incompatible.Errors[0].Category);

        var listed = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourcePropertyAssignmentManagementService>();
            return await service.GetResourcePropertiesAsync(1, CancellationToken.None);
        });

        Assert.True(listed.Succeeded);
        Assert.Single(listed.Value!.Properties);

        var removed = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourcePropertyAssignmentManagementService>();
            return await service.RemovePropertiesFromResourceAsync(
                new RemovePropertiesFromResourceCommand(1, new[] { 301 }),
                CancellationToken.None);
        });

        Assert.True(removed.Succeeded);
        Assert.Empty(removed.Value!.Properties);
    }

    [Fact]
    public async Task Operations_Require_Tenant_Context()
    {
        await using var provider = BuildServiceProvider();

        using var scope = provider.CreateScope();
        var propertyService = scope.ServiceProvider.GetRequiredService<IPropertyManagementService>();
        var assignmentService = scope.ServiceProvider.GetRequiredService<IResourcePropertyAssignmentManagementService>();

        var propertyResult = await propertyService.CreatePropertyAsync(
            new CreatePropertyCommand("K", "Label", null),
            CancellationToken.None);

        var assignmentResult = await assignmentService.AssignPropertiesToResourceAsync(
            new AssignPropertiesToResourceCommand(1, new[] { 100 }),
            CancellationToken.None);

        Assert.False(propertyResult.Succeeded);
        Assert.Equal("tenant.context.unresolved", propertyResult.Errors[0].Code);
        Assert.False(assignmentResult.Succeeded);
        Assert.Equal("tenant.context.unresolved", assignmentResult.Errors[0].Code);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = $"property-management-{Guid.NewGuid()}";

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddDbContext<SchedulerDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IPropertyManagementStore, PropertyManagementStore>();
        services.AddScoped<IResourcePropertyAssignmentManagementStore, ResourcePropertyAssignmentManagementStore>();
        services.AddScoped<IPropertyManagementService, PropertyManagementService>();
        services.AddScoped<IResourcePropertyAssignmentManagementService, ResourcePropertyAssignmentManagementService>();
        services.AddScoped<IResourceManagementStore, ResourceManagementStore>();
        services.AddScoped<IPropertySchemaQueryService, PropertySchemaQueryService>();
        services.AddScoped<IPropertySchemaService, PropertySchemaService>();

        return services.BuildServiceProvider();
    }

    private static async Task<TenantInfo> SeedCatalogAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var tenantId = new Guid("55555555-5555-5555-5555-555555555555");
        tenantContext.SetTenant(tenantId, "default");

        dbContext.Tenants.Add(new Tenants
        {
            Id = tenantId,
            Key = "default",
            Label = "Default",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 21, 12, 0, 0, DateTimeKind.Utc)
        });

        dbContext.ResourceTypes.AddRange(
            new ResourceTypes
            {
                Id = 1,
                TenantId = tenantId,
                Key = "room",
                Label = "Room",
                SortOrder = 1,
                IsActive = true
            },
            new ResourceTypes
            {
                Id = 2,
                TenantId = tenantId,
                Key = "device",
                Label = "Device",
                SortOrder = 2,
                IsActive = true
            });

        dbContext.Resources.AddRange(
            new Resources
            {
                Id = 1,
                TenantId = tenantId,
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
                TenantId = tenantId,
                Code = "D1",
                Name = "Device 1",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 2,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 12, 0, 0, DateTimeKind.Utc)
            });

        dbContext.ResourceProperties.AddRange(
            new ResourceProperties { Id = 100, TenantId = tenantId, Key = "Location", Label = "Location", ParentId = null, SortOrder = 1, IsActive = true },
            new ResourceProperties { Id = 101, TenantId = tenantId, Key = "Location", Label = "Milan", ParentId = 100, SortOrder = 1, IsActive = true },
            new ResourceProperties { Id = 102, TenantId = tenantId, Key = "Location", Label = "Rome", ParentId = null, SortOrder = 2, IsActive = true },
            new ResourceProperties { Id = 103, TenantId = tenantId, Key = "Feature", Label = "Feature", ParentId = null, SortOrder = 3, IsActive = true },
            new ResourceProperties { Id = 104, TenantId = tenantId, Key = "Feature", Label = "Portable", ParentId = null, SortOrder = 4, IsActive = true },
            new ResourceProperties { Id = 200, TenantId = tenantId, Key = "Sterility", Label = "Sterility", ParentId = null, SortOrder = 5, IsActive = true },
            new ResourceProperties { Id = 300, TenantId = tenantId, Key = "Capability", Label = "Capability", ParentId = null, SortOrder = 6, IsActive = true },
            new ResourceProperties { Id = 301, TenantId = tenantId, Key = "Capability", Label = "XRay", ParentId = 300, SortOrder = 1, IsActive = true });

        dbContext.ResourceTypeProperties.AddRange(
            new ResourceTypeProperties { TenantId = tenantId, ResourceTypeId = 1, PropertyDefinitionId = 100 },
            new ResourceTypeProperties { TenantId = tenantId, ResourceTypeId = 1, PropertyDefinitionId = 300 },
            new ResourceTypeProperties { TenantId = tenantId, ResourceTypeId = 2, PropertyDefinitionId = 200 });

        await dbContext.SaveChangesAsync();
        return new TenantInfo(tenantId, "default", "Default", true);
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
}
