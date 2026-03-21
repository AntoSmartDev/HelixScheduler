using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability.QueryServices;
using HelixScheduler.Application.Management;
using HelixScheduler.Application.ResourceCatalog.Management;
using HelixScheduler.Application.RuleManagement;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Entities;
using HelixScheduler.Infrastructure.Persistence.QueryServices;
using HelixScheduler.Infrastructure.Persistence.Stores;
using HelixScheduler.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class RuleManagementServiceTests
{
    [Fact]
    public async Task Create_Get_List_And_Deactivate_Work_For_All_Shapes()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedResourcesAsync(provider);

        var createdIds = new List<long>();
        foreach (var definition in BuildAllShapes())
        {
            var created = await ExecuteForTenantAsync(provider, tenant, async sp =>
            {
                var service = sp.GetRequiredService<IRuleManagementService>();
                return await service.CreateRuleAsync(new CreateRuleCommand(definition), CancellationToken.None);
            });

            Assert.True(created.Succeeded);
            Assert.True(created.Value!.IsActive);
            createdIds.Add(created.Value.Id);
        }

        var listed = await ExecuteForTenantAsync(provider, tenant, sp =>
            sp.GetRequiredService<IRuleManagementService>().ListRulesAsync(CancellationToken.None));

        Assert.Equal(5, listed.Count);

        var repeating = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IRuleManagementService>();
            return await service.GetRuleAsync(createdIds[^1], CancellationToken.None);
        });

        Assert.True(repeating.Succeeded);
        Assert.Equal(RuleShape.Repeating, repeating.Value!.Shape);

        var deactivated = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IRuleManagementService>();
            return await service.DeactivateRuleAsync(createdIds[0], CancellationToken.None);
        });

        Assert.True(deactivated.Succeeded);
        Assert.False(deactivated.Value!.IsActive);
    }

    [Fact]
    public async Task Update_Rule_Replaces_Shape_And_Resources()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedResourcesAsync(provider);

        var created = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IRuleManagementService>();
            return await service.CreateRuleAsync(
                new CreateRuleCommand(new RuleDefinition(
                    RuleShape.Weekly,
                    false,
                    new[] { 1 },
                    "Weekly room",
                    null,
                    null,
                    null,
                    new TimeOnly(9, 0),
                    new TimeOnly(12, 0),
                    2,
                    null,
                    null)),
                CancellationToken.None);
        });

        var updated = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IRuleManagementService>();
            return await service.UpdateRuleAsync(
                new UpdateRuleCommand(
                    created.Value!.Id,
                    new RuleDefinition(
                        RuleShape.Range,
                        true,
                        new[] { 1, 2 },
                        "Maintenance",
                        new DateOnly(2026, 3, 25),
                        new DateOnly(2026, 3, 28),
                        null,
                        new TimeOnly(13, 0),
                        new TimeOnly(16, 0),
                        127,
                        12,
                        5)),
                CancellationToken.None);
        });

        Assert.True(updated.Succeeded);
        Assert.Equal(RuleShape.Range, updated.Value!.Shape);
        Assert.True(updated.Value.IsExclude);
        Assert.Equal(2, updated.Value.ResourceIds.Count);
        Assert.Null(updated.Value.DaysOfWeekMask);
        Assert.Null(updated.Value.DayOfMonth);
        Assert.Null(updated.Value.IntervalDays);
    }

    [Fact]
    public async Task Validation_Rejects_Invalid_Shape_And_Inactive_Resources()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedResourcesAsync(provider);

        var invalidWeekly = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IRuleManagementService>();
            return await service.CreateRuleAsync(
                new CreateRuleCommand(new RuleDefinition(
                    RuleShape.Weekly,
                    false,
                    new[] { 1 },
                    null,
                    null,
                    null,
                    null,
                    new TimeOnly(10, 0),
                    new TimeOnly(9, 0),
                    null,
                    null,
                    null)),
                CancellationToken.None);
        });

        Assert.False(invalidWeekly.Succeeded);
        Assert.Contains(invalidWeekly.Errors, error => error.Code == "rule.time-range.invalid");
        Assert.Contains(invalidWeekly.Errors, error => error.Code == "rule.days-of-week.invalid");

        var inactiveResource = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var db = sp.GetRequiredService<SchedulerDbContext>();
            var resource = await db.Resources.FirstAsync(item => item.Id == 2);
            resource.IsActive = false;
            await db.SaveChangesAsync();

            var service = sp.GetRequiredService<IRuleManagementService>();
            return await service.CreateRuleAsync(
                new CreateRuleCommand(new RuleDefinition(
                    RuleShape.SingleDate,
                    false,
                    new[] { 2 },
                    null,
                    null,
                    null,
                    new DateOnly(2026, 3, 30),
                    new TimeOnly(10, 0),
                    new TimeOnly(11, 0),
                    null,
                    null,
                    null)),
                CancellationToken.None);
        });

        Assert.False(inactiveResource.Succeeded);
        Assert.Equal("rule.resource.inactive", inactiveResource.Errors[0].Code);
        Assert.Equal(ManagementErrorCategory.InvalidOperation, inactiveResource.Errors[0].Category);
    }

    [Fact]
    public async Task Inactive_Rules_Are_Excluded_From_Compute_And_Summary_Read_Side()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedResourcesAsync(provider);

        var created = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IRuleManagementService>();
            return await service.CreateRuleAsync(
                new CreateRuleCommand(new RuleDefinition(
                    RuleShape.Monthly,
                    false,
                    new[] { 1 },
                    "Month rule",
                    null,
                    null,
                    null,
                    new TimeOnly(9, 0),
                    new TimeOnly(10, 0),
                    null,
                    15,
                    null)),
                CancellationToken.None);
        });

        await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IRuleManagementService>();
            return await service.DeactivateRuleAsync(created.Value!.Id, CancellationToken.None);
        });

        var computeRules = await ExecuteForTenantAsync(provider, tenant, sp =>
            sp.GetRequiredService<IAvailabilityComputeQueryService>().GetRulesAsync(
                new DateOnly(2026, 3, 1),
                new DateOnly(2026, 3, 31),
                new[] { 1 },
                CancellationToken.None));

        Assert.Empty(computeRules);

        var summaries = await ExecuteForTenantAsync(provider, tenant, sp =>
            sp.GetRequiredService<IAvailabilitySummaryQueryService>().GetRuleSummariesAsync(
                new DateOnly(2026, 3, 1),
                new DateOnly(2026, 3, 31),
                new[] { 1 },
                CancellationToken.None));

        Assert.Empty(summaries);
    }

    [Fact]
    public async Task Operations_Require_Tenant_Context()
    {
        await using var provider = BuildServiceProvider();

        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRuleManagementService>();

        var result = await service.CreateRuleAsync(
            new CreateRuleCommand(new RuleDefinition(
                RuleShape.SingleDate,
                false,
                new[] { 1 },
                null,
                null,
                null,
                new DateOnly(2026, 3, 30),
                new TimeOnly(10, 0),
                new TimeOnly(11, 0),
                null,
                null,
                null)),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("tenant.context.unresolved", result.Errors[0].Code);
    }

    private static IReadOnlyList<RuleDefinition> BuildAllShapes()
    {
        return
        [
            new RuleDefinition(RuleShape.Weekly, false, new[] { 1 }, "Weekly", null, null, null, new TimeOnly(9, 0), new TimeOnly(12, 0), 2, null, null),
            new RuleDefinition(RuleShape.Monthly, false, new[] { 1 }, "Monthly", null, null, null, new TimeOnly(9, 0), new TimeOnly(12, 0), null, 15, null),
            new RuleDefinition(RuleShape.SingleDate, false, new[] { 1 }, "Single", null, null, new DateOnly(2026, 3, 30), new TimeOnly(9, 0), new TimeOnly(12, 0), null, null, null),
            new RuleDefinition(RuleShape.Range, true, new[] { 1, 2 }, "Range", new DateOnly(2026, 3, 25), new DateOnly(2026, 3, 28), null, new TimeOnly(13, 0), new TimeOnly(16, 0), null, null, null),
            new RuleDefinition(RuleShape.Repeating, false, new[] { 2 }, "Repeating", new DateOnly(2026, 3, 20), null, null, new TimeOnly(8, 0), new TimeOnly(9, 0), null, null, 3)
        ];
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = $"rule-management-{Guid.NewGuid()}";

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddSingleton<IClock>(new FixedClock(new DateTime(2026, 3, 21, 13, 0, 0, DateTimeKind.Utc)));
        services.AddDbContext<SchedulerDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IResourceManagementStore, ResourceManagementStore>();
        services.AddScoped<IRuleManagementStore, RuleManagementStore>();
        services.AddScoped<IRuleManagementService, RuleManagementService>();
        services.AddScoped<IAvailabilityComputeQueryService, AvailabilityComputeQueryService>();
        services.AddScoped<IAvailabilitySummaryQueryService, AvailabilitySummaryQueryService>();

        return services.BuildServiceProvider();
    }

    private static async Task<TenantInfo> SeedResourcesAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var tenantId = new Guid("66666666-6666-6666-6666-666666666666");
        tenantContext.SetTenant(tenantId, "default");

        dbContext.Tenants.Add(new Tenants
        {
            Id = tenantId,
            Key = "default",
            Label = "Default",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 21, 13, 0, 0, DateTimeKind.Utc)
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
                CreatedAtUtc = new DateTime(2026, 3, 21, 13, 0, 0, DateTimeKind.Utc)
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
                CreatedAtUtc = new DateTime(2026, 3, 21, 13, 0, 0, DateTimeKind.Utc)
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
