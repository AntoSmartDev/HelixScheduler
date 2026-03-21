using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;
using HelixScheduler.Application.Tenancy;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Stores;
using HelixScheduler.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class TenantManagementServiceTests
{
    [Fact]
    public async Task Create_Get_List_Update_And_Lifecycle_Work()
    {
        await using var provider = BuildServiceProvider();
        await SeedDefaultTenantAsync(provider);

        var created = await ExecuteAsync(provider, service =>
            service.CreateTenantAsync(new CreateTenantCommand("tenant-a", "Tenant A"), CancellationToken.None));

        Assert.True(created.Succeeded);
        Assert.NotNull(created.Value);
        Assert.True(created.Value!.IsActive);
        Assert.Equal("tenant-a", created.Value.Key);

        var loaded = await ExecuteAsync(provider, service =>
            service.GetTenantAsync(created.Value.Id, CancellationToken.None));

        Assert.True(loaded.Succeeded);
        Assert.Equal(created.Value.Id, loaded.Value!.Id);

        var listed = await ExecuteAsync(provider, service =>
            service.ListTenantsAsync(CancellationToken.None));

        Assert.Equal(2, listed.Count);

        var updated = await ExecuteAsync(provider, service =>
            service.UpdateTenantAsync(
                new UpdateTenantCommand(created.Value.Id, "tenant-a-renamed", "Tenant A Renamed"),
                CancellationToken.None));

        Assert.True(updated.Succeeded);
        Assert.Equal("tenant-a-renamed", updated.Value!.Key);
        Assert.Equal("Tenant A Renamed", updated.Value.Label);

        var deactivated = await ExecuteAsync(provider, service =>
            service.DeactivateTenantAsync(created.Value.Id, CancellationToken.None));

        Assert.True(deactivated.Succeeded);
        Assert.False(deactivated.Value!.IsActive);

        var activated = await ExecuteAsync(provider, service =>
            service.ActivateTenantAsync(created.Value.Id, CancellationToken.None));

        Assert.True(activated.Succeeded);
        Assert.True(activated.Value!.IsActive);
    }

    [Fact]
    public async Task CreateTenant_ReturnsConflict_ForDuplicateKey()
    {
        await using var provider = BuildServiceProvider();
        await SeedDefaultTenantAsync(provider);

        var first = await ExecuteAsync(provider, service =>
            service.CreateTenantAsync(new CreateTenantCommand("tenant-a", "Tenant A"), CancellationToken.None));
        var second = await ExecuteAsync(provider, service =>
            service.CreateTenantAsync(new CreateTenantCommand("tenant-a", "Another Tenant A"), CancellationToken.None));

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Single(second.Errors);
        Assert.Equal("tenant.key.duplicate", second.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.Conflict, second.Errors[0].Category);
    }

    [Fact]
    public async Task GetTenant_ReturnsNotFound_WhenMissing()
    {
        await using var provider = BuildServiceProvider();
        await SeedDefaultTenantAsync(provider);

        var result = await ExecuteAsync(provider, service =>
            service.GetTenantAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("tenant.not-found", result.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.NotFound, result.Errors[0].Category);
    }

    [Fact]
    public async Task Lifecycle_ReturnsInvalidOperation_ForAlreadyActive_And_DefaultDeactivation()
    {
        await using var provider = BuildServiceProvider();
        var defaultTenant = await SeedDefaultTenantAsync(provider);

        var activateDefault = await ExecuteAsync(provider, service =>
            service.ActivateTenantAsync(defaultTenant.Id, CancellationToken.None));

        Assert.False(activateDefault.Succeeded);
        Assert.Single(activateDefault.Errors);
        Assert.Equal("tenant.lifecycle.already-active", activateDefault.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.InvalidOperation, activateDefault.Errors[0].Category);

        var deactivateDefault = await ExecuteAsync(provider, service =>
            service.DeactivateTenantAsync(defaultTenant.Id, CancellationToken.None));

        Assert.False(deactivateDefault.Succeeded);
        Assert.Single(deactivateDefault.Errors);
        Assert.Equal("tenant.lifecycle.default-deactivation-forbidden", deactivateDefault.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.InvalidOperation, deactivateDefault.Errors[0].Category);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = $"tenant-management-{Guid.NewGuid()}";
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 3, 21, 10, 0, 0, DateTimeKind.Utc)));
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddDbContext<SchedulerDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<ITenantManagementStore, TenantManagementStore>();
        services.AddScoped<ITenantManagementService, TenantManagementService>();
        return services.BuildServiceProvider();
    }

    private static async Task<TenantInfo> SeedDefaultTenantAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>();
        return await tenantStore.EnsureDefaultAsync(CancellationToken.None);
    }

    private static async Task<T> ExecuteAsync<T>(ServiceProvider provider, Func<ITenantManagementService, Task<T>> action)
    {
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITenantManagementService>();
        return await action(service);
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
