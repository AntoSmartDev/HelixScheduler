using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability.QueryServices;
using HelixScheduler.Application.BusyEventManagement;
using HelixScheduler.Application.Management;
using HelixScheduler.Application.ResourceCatalog.Management;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Entities;
using HelixScheduler.Infrastructure.Persistence.QueryServices;
using HelixScheduler.Infrastructure.Persistence.Stores;
using HelixScheduler.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class BusyEventManagementServiceTests
{
    [Fact]
    public async Task Register_Get_List_Update_And_Cancel_Work()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedResourcesAsync(provider);

        var registered = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IBusyEventManagementService>();
            return await service.RegisterBusyEventAsync(
                new RegisterBusyEventCommand(new BusyEventDefinition(
                    new[] { 1, 2 },
                    new DateTime(2026, 3, 25, 8, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 3, 25, 10, 0, 0, DateTimeKind.Utc),
                    "Surgery block",
                    "Reservation")),
                CancellationToken.None);
        });

        Assert.True(registered.Succeeded);
        Assert.Equal(2, registered.Value!.ResourceIds.Count);
        Assert.True(registered.Value.IsActive);

        var fetched = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IBusyEventManagementService>();
            return await service.GetBusyEventAsync(registered.Value!.Id, CancellationToken.None);
        });

        Assert.True(fetched.Succeeded);
        Assert.Equal("Surgery block", fetched.Value!.Title);

        var listed = await ExecuteForTenantAsync(provider, tenant, sp =>
            sp.GetRequiredService<IBusyEventManagementService>().ListBusyEventsAsync(CancellationToken.None));

        Assert.Single(listed);

        var updated = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IBusyEventManagementService>();
            return await service.UpdateBusyEventAsync(
                new UpdateBusyEventCommand(
                    registered.Value!.Id,
                    new BusyEventDefinition(
                        new[] { 2 },
                        new DateTime(2026, 3, 25, 11, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc),
                        "Updated busy",
                        "Maintenance")),
                CancellationToken.None);
        });

        Assert.True(updated.Succeeded);
        Assert.Single(updated.Value!.ResourceIds);
        Assert.Equal(2, updated.Value.ResourceIds[0]);
        Assert.Equal("Updated busy", updated.Value.Title);

        var canceled = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IBusyEventManagementService>();
            return await service.CancelBusyEventAsync(registered.Value!.Id, CancellationToken.None);
        });

        Assert.True(canceled.Succeeded);
        Assert.False(canceled.Value!.IsActive);
    }

    [Fact]
    public async Task Validation_Rejects_NonUtc_InvalidRange_And_Inactive_Resources()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedResourcesAsync(provider);

        var invalid = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IBusyEventManagementService>();
            return await service.RegisterBusyEventAsync(
                new RegisterBusyEventCommand(new BusyEventDefinition(
                    new[] { 1 },
                    new DateTime(2026, 3, 25, 10, 0, 0, DateTimeKind.Local),
                    new DateTime(2026, 3, 25, 9, 0, 0, DateTimeKind.Utc),
                    null,
                    null)),
                CancellationToken.None);
        });

        Assert.False(invalid.Succeeded);
        Assert.Contains(invalid.Errors, error => error.Code == "busy-event.utc.required");
        Assert.Contains(invalid.Errors, error => error.Code == "busy-event.time-range.invalid");

        var inactiveResource = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var db = sp.GetRequiredService<SchedulerDbContext>();
            var resource = await db.Resources.FirstAsync(item => item.Id == 2);
            resource.IsActive = false;
            await db.SaveChangesAsync();

            var service = sp.GetRequiredService<IBusyEventManagementService>();
            return await service.RegisterBusyEventAsync(
                new RegisterBusyEventCommand(new BusyEventDefinition(
                    new[] { 2 },
                    new DateTime(2026, 3, 25, 10, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 3, 25, 11, 0, 0, DateTimeKind.Utc),
                    null,
                    null)),
                CancellationToken.None);
        });

        Assert.False(inactiveResource.Succeeded);
        Assert.Equal("busy-event.resource.inactive", inactiveResource.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.InvalidOperation, inactiveResource.Errors[0].Category);
    }

    [Fact]
    public async Task Inactive_Busy_Events_Are_Excluded_From_Compute_Path()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedResourcesAsync(provider);

        var registered = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IBusyEventManagementService>();
            return await service.RegisterBusyEventAsync(
                new RegisterBusyEventCommand(new BusyEventDefinition(
                    new[] { 1, 2 },
                    new DateTime(2026, 3, 26, 8, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 3, 26, 9, 0, 0, DateTimeKind.Utc),
                    "Busy compute",
                    "Occupied")),
                CancellationToken.None);
        });

        await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IBusyEventManagementService>();
            return await service.CancelBusyEventAsync(registered.Value!.Id, CancellationToken.None);
        });

        var busyEvents = await ExecuteForTenantAsync(provider, tenant, sp =>
            sp.GetRequiredService<IAvailabilityComputeQueryService>().GetBusyEventsAsync(
                new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc),
                new[] { 1, 2 },
                CancellationToken.None));

        Assert.Empty(busyEvents);
    }

    [Fact]
    public async Task Operations_Require_Tenant_Context()
    {
        await using var provider = BuildServiceProvider();

        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBusyEventManagementService>();

        var result = await service.RegisterBusyEventAsync(
            new RegisterBusyEventCommand(new BusyEventDefinition(
                new[] { 1 },
                new DateTime(2026, 3, 25, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 25, 11, 0, 0, DateTimeKind.Utc),
                null,
                null)),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("tenant.context.unresolved", result.Errors[0].Code);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = $"busy-event-management-{Guid.NewGuid()}";

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddSingleton<IClock>(new FixedClock(new DateTime(2026, 3, 21, 14, 0, 0, DateTimeKind.Utc)));
        services.AddDbContext<SchedulerDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IResourceManagementStore, ResourceManagementStore>();
        services.AddScoped<IBusyEventManagementStore, BusyEventManagementStore>();
        services.AddScoped<IBusyEventManagementService, BusyEventManagementService>();
        services.AddScoped<IAvailabilityComputeQueryService, AvailabilityComputeQueryService>();

        return services.BuildServiceProvider();
    }

    private static async Task<TenantInfo> SeedResourcesAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var tenantId = new Guid("77777777-7777-7777-7777-777777777777");
        tenantContext.SetTenant(tenantId, "default");

        dbContext.Tenants.Add(new Tenants
        {
            Id = tenantId,
            Key = "default",
            Label = "Default",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 21, 14, 0, 0, DateTimeKind.Utc)
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
                Code = "R1",
                Name = "Room 1",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 14, 0, 0, DateTimeKind.Utc)
            },
            new Resources
            {
                Id = 2,
                TenantId = tenantId,
                Code = "R2",
                Name = "Room 2",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 14, 0, 0, DateTimeKind.Utc)
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

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
