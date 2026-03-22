using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;
using HelixScheduler.Application.ResourceCatalog;
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

public sealed class ResourceCatalogManagementServiceTests
{
    [Fact]
    public async Task ResourceType_And_Resource_Management_Work_EndToEnd()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedTenantsAsync(provider);

        var typeCreated = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.CreateResourceTypeAsync(
                new CreateResourceTypeCommand("room", "Room", 1),
                CancellationToken.None);
        });

        Assert.True(typeCreated.Succeeded);
        Assert.True(typeCreated.Value!.IsActive);

        var resourceCreated = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceManagementService>();
            return await service.CreateResourceAsync(
                new CreateResourceCommand("ROOM-A", "Room A", true, 2, typeCreated.Value!.Id),
                CancellationToken.None);
        });

        Assert.True(resourceCreated.Succeeded);
        Assert.Equal(2, resourceCreated.Value!.Capacity);
        Assert.True(resourceCreated.Value.IsActive);
        Assert.False(resourceCreated.Value.IsArchived);

        var typeLoaded = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.GetResourceTypeAsync(typeCreated.Value!.Id, CancellationToken.None);
        });

        Assert.True(typeLoaded.Succeeded);

        var resourceLoaded = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceManagementService>();
            return await service.GetResourceAsync(resourceCreated.Value!.Id, CancellationToken.None);
        });

        Assert.True(resourceLoaded.Succeeded);
        Assert.Equal("ROOM-A", resourceLoaded.Value!.Code);

        var typesListed = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, sp =>
            sp.GetRequiredService<IResourceTypeManagementService>().ListResourceTypesAsync(CancellationToken.None));
        var resourcesListed = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, sp =>
            sp.GetRequiredService<IResourceManagementService>().ListResourcesAsync(CancellationToken.None));

        Assert.Single(typesListed);
        Assert.Single(resourcesListed);

        var typeUpdated = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.UpdateResourceTypeAsync(
                new UpdateResourceTypeCommand(typeCreated.Value!.Id, "room-updated", "Room Updated", 5),
                CancellationToken.None);
        });

        var resourceUpdated = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceManagementService>();
            return await service.UpdateResourceAsync(
                new UpdateResourceCommand(resourceCreated.Value!.Id, "ROOM-A2", "Room A2", false, 3, typeCreated.Value!.Id),
                CancellationToken.None);
        });

        Assert.True(typeUpdated.Succeeded);
        Assert.Equal("room-updated", typeUpdated.Value!.Key);
        Assert.True(resourceUpdated.Succeeded);
        Assert.Equal("ROOM-A2", resourceUpdated.Value!.Code);
        Assert.False(resourceUpdated.Value.IsSchedulable);

        var resourceDeactivated = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceManagementService>();
            return await service.DeactivateResourceAsync(resourceCreated.Value!.Id, CancellationToken.None);
        });

        Assert.True(resourceDeactivated.Succeeded);
        Assert.False(resourceDeactivated.Value!.IsActive);

        var resourceActivated = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceManagementService>();
            return await service.ActivateResourceAsync(resourceCreated.Value!.Id, CancellationToken.None);
        });

        Assert.True(resourceActivated.Succeeded);
        Assert.True(resourceActivated.Value!.IsActive);

        var archived = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceManagementService>();
            return await service.ArchiveResourceAsync(resourceCreated.Value!.Id, CancellationToken.None);
        });

        Assert.True(archived.Succeeded);
        Assert.True(archived.Value!.IsArchived);
        Assert.False(archived.Value.IsActive);

        var typeDeactivated = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.DeactivateResourceTypeAsync(typeCreated.Value!.Id, CancellationToken.None);
        });

        Assert.True(typeDeactivated.Succeeded);
        Assert.False(typeDeactivated.Value!.IsActive);
    }

    [Fact]
    public async Task Management_Validation_And_Catalog_Read_Boundary_Are_Enforced()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedTenantsAsync(provider);

        var createdType = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.CreateResourceTypeAsync(
                new CreateResourceTypeCommand("room", "Room", 1),
                CancellationToken.None);
        });

        var duplicateType = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.CreateResourceTypeAsync(
                new CreateResourceTypeCommand("room", "Room Duplicate", 2),
                CancellationToken.None);
        });

        Assert.True(createdType.Succeeded);
        Assert.False(duplicateType.Succeeded);
        Assert.Equal("resource-type.key.duplicate", duplicateType.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.Conflict, duplicateType.Errors[0].Category);

        var secondTenantResult = await ExecuteForTenantAsync(provider, tenant.OtherTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.CreateResourceTypeAsync(
                new CreateResourceTypeCommand("room", "Room Other Tenant", 1),
                CancellationToken.None);
        });

        Assert.True(secondTenantResult.Succeeded);

        var missingTypeResource = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceManagementService>();
            return await service.CreateResourceAsync(
                new CreateResourceCommand("ROOM-MISSING", "Room Missing", true, 1, 9999),
                CancellationToken.None);
        });

        Assert.False(missingTypeResource.Succeeded);
        Assert.Equal("resource.type.not-found", missingTypeResource.Errors[0].Code);

        var resourceCreated = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceManagementService>();
            return await service.CreateResourceAsync(
                new CreateResourceCommand("ROOM-A", "Room A", true, 1, createdType.Value!.Id),
                CancellationToken.None);
        });

        Assert.True(resourceCreated.Succeeded);

        var deactivateTypeWithActiveResources = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.DeactivateResourceTypeAsync(createdType.Value!.Id, CancellationToken.None);
        });

        Assert.False(deactivateTypeWithActiveResources.Succeeded);
        Assert.Equal("resource-type.lifecycle.active-resources-exist", deactivateTypeWithActiveResources.Errors[0].Code);

        var archivedResource = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceManagementService>();
            return await service.ArchiveResourceAsync(resourceCreated.Value!.Id, CancellationToken.None);
        });

        Assert.True(archivedResource.Succeeded);

        var deactivatedType = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.DeactivateResourceTypeAsync(createdType.Value!.Id, CancellationToken.None);
        });

        Assert.True(deactivatedType.Succeeded);

        var createWithInactiveType = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceManagementService>();
            return await service.CreateResourceAsync(
                new CreateResourceCommand("ROOM-B", "Room B", true, 1, createdType.Value!.Id),
                CancellationToken.None);
        });

        Assert.False(createWithInactiveType.Succeeded);
        Assert.Equal("resource.type.inactive", createWithInactiveType.Errors[0].Code);

        var readCatalog = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var resourceCatalog = sp.GetRequiredService<IResourceCatalogService>();
            var resourceTypeCatalog = sp.GetRequiredService<IResourceTypeCatalogService>();
            var resources = await resourceCatalog.GetResourcesAsync(false, CancellationToken.None);
            var resourceTypes = await resourceTypeCatalog.GetResourceTypesAsync(CancellationToken.None);
            return (resources, resourceTypes);
        });

        Assert.Empty(readCatalog.resources);
        Assert.Empty(readCatalog.resourceTypes);
    }

    [Fact]
    public async Task ResourceType_And_Resource_Operations_Require_Tenant_Context()
    {
        await using var provider = BuildServiceProvider();

        using var scope = provider.CreateScope();
        var resourceTypeService = scope.ServiceProvider.GetRequiredService<IResourceTypeManagementService>();
        var resourceService = scope.ServiceProvider.GetRequiredService<IResourceManagementService>();

        var typeResult = await resourceTypeService.CreateResourceTypeAsync(
            new CreateResourceTypeCommand("room", "Room", 1),
            CancellationToken.None);

        var resourceResult = await resourceService.CreateResourceAsync(
            new CreateResourceCommand("ROOM-A", "Room A", true, 1, 1),
            CancellationToken.None);

        Assert.False(typeResult.Succeeded);
        Assert.Equal("tenant.context.unresolved", typeResult.Errors[0].Code);
        Assert.False(resourceResult.Succeeded);
        Assert.Equal("tenant.context.unresolved", resourceResult.Errors[0].Code);
    }

    [Fact]
    public async Task ResourceType_PropertySchema_Management_Assigns_And_Removes_Definitions()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedTenantsAsync(provider);
        await SeedPropertyDefinitionsAsync(provider, tenant.CurrentTenant.Id);

        var createdType = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.CreateResourceTypeAsync(
                new CreateResourceTypeCommand("room", "Room", 1),
                CancellationToken.None);
        });

        Assert.True(createdType.Succeeded);

        var assigned = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypePropertySchemaManagementService>();
            return await service.AssignPropertyDefinitionsAsync(
                new AssignPropertyDefinitionsToResourceTypeCommand(createdType.Value!.Id, new[] { 100, 200 }),
                CancellationToken.None);
        });

        Assert.True(assigned.Succeeded);
        Assert.Equal(new[] { 100, 200 }, assigned.Value!.PropertyDefinitionIds);

        var duplicate = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypePropertySchemaManagementService>();
            return await service.AssignPropertyDefinitionsAsync(
                new AssignPropertyDefinitionsToResourceTypeCommand(createdType.Value!.Id, new[] { 100 }),
                CancellationToken.None);
        });

        Assert.False(duplicate.Succeeded);
        Assert.Equal("resource-type.property-definition.duplicate", duplicate.Errors[0].Code);

        var removed = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypePropertySchemaManagementService>();
            return await service.RemovePropertyDefinitionsAsync(
                new RemovePropertyDefinitionsFromResourceTypeCommand(createdType.Value!.Id, new[] { 100 }),
                CancellationToken.None);
        });

        Assert.True(removed.Succeeded);
        Assert.Equal(new[] { 200 }, removed.Value!.PropertyDefinitionIds);
    }

    [Fact]
    public async Task ResourceType_PropertySchema_Management_Rejects_Missing_And_Inactive_Definitions()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedTenantsAsync(provider);
        await SeedPropertyDefinitionsAsync(provider, tenant.CurrentTenant.Id);

        var createdType = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypeManagementService>();
            return await service.CreateResourceTypeAsync(
                new CreateResourceTypeCommand("device", "Device", 1),
                CancellationToken.None);
        });

        Assert.True(createdType.Succeeded);

        var missing = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypePropertySchemaManagementService>();
            return await service.AssignPropertyDefinitionsAsync(
                new AssignPropertyDefinitionsToResourceTypeCommand(createdType.Value!.Id, new[] { 999 }),
                CancellationToken.None);
        });

        Assert.False(missing.Succeeded);
        Assert.Equal("resource-type.property-definition.not-found", missing.Errors[0].Code);

        var inactive = await ExecuteForTenantAsync(provider, tenant.CurrentTenant, async sp =>
        {
            var service = sp.GetRequiredService<IResourceTypePropertySchemaManagementService>();
            return await service.AssignPropertyDefinitionsAsync(
                new AssignPropertyDefinitionsToResourceTypeCommand(createdType.Value!.Id, new[] { 300 }),
                CancellationToken.None);
        });

        Assert.False(inactive.Succeeded);
        Assert.Equal("resource-type.property-definition.inactive", inactive.Errors[0].Code);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = $"resource-management-{Guid.NewGuid()}";

        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc)));
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddDbContext<SchedulerDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<IResourceTypeManagementStore, ResourceTypeManagementStore>();
        services.AddScoped<IResourceTypePropertySchemaManagementStore, ResourceTypePropertySchemaManagementStore>();
        services.AddScoped<IResourceManagementStore, ResourceManagementStore>();
        services.AddScoped<IResourceTypeManagementService, ResourceTypeManagementService>();
        services.AddScoped<IResourceTypePropertySchemaManagementService, ResourceTypePropertySchemaManagementService>();
        services.AddScoped<IResourceManagementService, ResourceManagementService>();
        services.AddScoped<IResourceCatalogQueryService, ResourceCatalogQueryService>();
        services.AddScoped<IResourceTypeCatalogQueryService, ResourceTypeCatalogQueryService>();
        services.AddScoped<IResourceCatalogService, ResourceCatalogService>();
        services.AddScoped<IResourceTypeCatalogService, ResourceTypeCatalogService>();

        return services.BuildServiceProvider();
    }

    private static async Task SeedPropertyDefinitionsAsync(ServiceProvider provider, Guid tenantId)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        if (await dbContext.ResourceProperties.AnyAsync(property => property.TenantId == tenantId))
        {
            return;
        }

        dbContext.ResourceProperties.AddRange(
            new ResourceProperties { Id = 100, TenantId = tenantId, Key = "Capability", Label = "Capability", SortOrder = 1, IsActive = true },
            new ResourceProperties { Id = 200, TenantId = tenantId, Key = "Specialty", Label = "Specialty", SortOrder = 2, IsActive = true },
            new ResourceProperties { Id = 300, TenantId = tenantId, Key = "Legacy", Label = "Legacy", SortOrder = 3, IsActive = false });

        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<TestTenantContext> SeedTenantsAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var current = await tenantStore.EnsureDefaultAsync(CancellationToken.None);
        var other = new Tenants
        {
            Id = new Guid("33333333-3333-3333-3333-333333333333"),
            Key = "tenant-b",
            Label = "Tenant B",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc)
        };

        dbContext.Tenants.Add(other);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return new TestTenantContext(current, new TenantInfo(other.Id, other.Key, other.Label, other.IsActive));
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

    private sealed record TestTenantContext(TenantInfo CurrentTenant, TenantInfo OtherTenant);

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
