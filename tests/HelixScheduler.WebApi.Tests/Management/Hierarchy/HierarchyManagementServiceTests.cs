using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management.Hierarchy;
using HelixScheduler.Application.Management;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Entities;
using HelixScheduler.Infrastructure.Persistence.Stores;
using HelixScheduler.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class HierarchyManagementServiceTests
{
    [Fact]
    public async Task Add_Remove_And_List_Relations_Work()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedGraphAsync(provider);

        var added = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IHierarchyManagementService>();
            return await service.AddParentRelationAsync(
                new AddParentRelationCommand(2, 3, "Contains"),
                CancellationToken.None);
        });

        Assert.True(added.Succeeded);
        Assert.Equal(2, added.Value!.ParentResourceId);
        Assert.Equal(3, added.Value.ChildResourceId);

        var listed = await ExecuteForTenantAsync(provider, tenant, sp =>
            sp.GetRequiredService<IHierarchyManagementService>().GetHierarchyRelationsAsync(CancellationToken.None));

        Assert.Single(listed);

        var removed = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IHierarchyManagementService>();
            return await service.RemoveParentRelationAsync(
                new RemoveParentRelationCommand(2, 3, "Contains"),
                CancellationToken.None);
        });

        Assert.True(removed.Succeeded);

        var listedAfterRemove = await ExecuteForTenantAsync(provider, tenant, sp =>
            sp.GetRequiredService<IHierarchyManagementService>().GetHierarchyRelationsAsync(CancellationToken.None));

        Assert.Empty(listedAfterRemove);
    }

    [Fact]
    public async Task Validation_Errors_Are_Enforced()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedGraphAsync(provider);

        var selfParent = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IHierarchyManagementService>();
            return await service.AddParentRelationAsync(
                new AddParentRelationCommand(1, 1, "Contains"),
                CancellationToken.None);
        });

        Assert.False(selfParent.Succeeded);
        Assert.Equal("hierarchy.self-parent-not-allowed", selfParent.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.Validation, selfParent.Errors[0].Category);

        var missingParent = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IHierarchyManagementService>();
            return await service.AddParentRelationAsync(
                new AddParentRelationCommand(999, 2, "Contains"),
                CancellationToken.None);
        });

        Assert.False(missingParent.Succeeded);
        Assert.Equal("hierarchy.parent-resource-not-found", missingParent.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.NotFound, missingParent.Errors[0].Category);
    }

    [Fact]
    public async Task Duplicate_And_Cycle_Are_Rejected()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedGraphAsync(provider);

        await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IHierarchyManagementService>();
            return await service.AddParentRelationAsync(
                new AddParentRelationCommand(1, 2, "Contains"),
                CancellationToken.None);
        });

        var duplicate = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IHierarchyManagementService>();
            return await service.AddParentRelationAsync(
                new AddParentRelationCommand(1, 2, "Contains"),
                CancellationToken.None);
        });

        Assert.False(duplicate.Succeeded);
        Assert.Equal("hierarchy.relation-already-exists", duplicate.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.Conflict, duplicate.Errors[0].Category);

        await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IHierarchyManagementService>();
            return await service.AddParentRelationAsync(
                new AddParentRelationCommand(2, 3, "Contains"),
                CancellationToken.None);
        });

        var cycle = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IHierarchyManagementService>();
            return await service.AddParentRelationAsync(
                new AddParentRelationCommand(3, 1, "Contains"),
                CancellationToken.None);
        });

        Assert.False(cycle.Succeeded);
        Assert.Equal("hierarchy.cycle-detected", cycle.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.InvalidOperation, cycle.Errors[0].Category);
    }

    [Fact]
    public async Task Inactive_Or_Archived_Resources_Cannot_Be_Used_In_Relations()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedGraphAsync(provider);

        var inactiveParent = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var db = sp.GetRequiredService<SchedulerDbContext>();
            var parent = await db.Resources.FirstAsync(resource => resource.Id == 1);
            parent.IsActive = false;
            await db.SaveChangesAsync();

            var service = sp.GetRequiredService<IHierarchyManagementService>();
            return await service.AddParentRelationAsync(
                new AddParentRelationCommand(1, 2, "Contains"),
                CancellationToken.None);
        });

        Assert.False(inactiveParent.Succeeded);
        Assert.Equal("hierarchy.parent-resource-inactive", inactiveParent.Errors[0].Code);

        var archivedChild = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var db = sp.GetRequiredService<SchedulerDbContext>();
            var parent = await db.Resources.FirstAsync(resource => resource.Id == 1);
            parent.IsActive = true;
            var child = await db.Resources.FirstAsync(resource => resource.Id == 2);
            child.IsArchived = true;
            await db.SaveChangesAsync();

            var service = sp.GetRequiredService<IHierarchyManagementService>();
            return await service.AddParentRelationAsync(
                new AddParentRelationCommand(1, 2, "Contains"),
                CancellationToken.None);
        });

        Assert.False(archivedChild.Succeeded);
        Assert.Equal("hierarchy.child-resource-inactive", archivedChild.Errors[0].Code);
    }

    [Fact]
    public async Task Operations_Require_Tenant_Context()
    {
        await using var provider = BuildServiceProvider();

        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IHierarchyManagementService>();

        var result = await service.AddParentRelationAsync(
            new AddParentRelationCommand(1, 2, "Contains"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("tenant.context.unresolved", result.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.InvalidOperation, result.Errors[0].Category);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = $"hierarchy-management-{Guid.NewGuid()}";

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddDbContext<SchedulerDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IHierarchyManagementStore, HierarchyManagementStore>();
        services.AddScoped<IHierarchyManagementService, HierarchyManagementService>();

        return services.BuildServiceProvider();
    }

    private static async Task<TenantInfo> SeedGraphAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var tenantId = new Guid("44444444-4444-4444-4444-444444444444");
        tenantContext.SetTenant(tenantId, "default");

        dbContext.Tenants.Add(new Tenants
        {
            Id = tenantId,
            Key = "default",
            Label = "Default",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc)
        });

        dbContext.ResourceTypes.Add(new ResourceTypes
        {
            Id = 1,
            TenantId = tenantId,
            Key = "room",
            Label = "Room",
            SortOrder = 1,
            IsActive = true
        });

        dbContext.Resources.AddRange(
            new Resources
            {
                Id = 1,
                TenantId = tenantId,
                Code = "PARENT",
                Name = "Parent",
                IsSchedulable = false,
                Capacity = 1,
                TypeId = 1,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc)
            },
            new Resources
            {
                Id = 2,
                TenantId = tenantId,
                Code = "CHILD",
                Name = "Child",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc)
            },
            new Resources
            {
                Id = 3,
                TenantId = tenantId,
                Code = "GRANDCHILD",
                Name = "Grandchild",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc)
            });

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
