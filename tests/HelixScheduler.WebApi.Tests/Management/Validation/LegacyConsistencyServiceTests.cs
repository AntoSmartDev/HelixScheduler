using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management.Validation;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Entities;
using HelixScheduler.Infrastructure.Persistence.QueryServices;
using HelixScheduler.Infrastructure.Persistence.Stores.Management.Validation;
using HelixScheduler.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class LegacyConsistencyServiceTests
{
    [Fact]
    public async Task Report_Exposes_Repairable_Inactive_Property_References_And_Validation_Findings()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedLegacyTenantAsync(provider);

        var result = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<ILegacyConsistencyService>();
            return await service.GetLegacyConsistencyReportAsync(CancellationToken.None);
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Contains(result.Value!.Validation.Findings, finding => finding.Code == "validation.resource.assigned-property-inactive");
        Assert.Contains(result.Value.Validation.Findings, finding => finding.Code == "validation.resource-type.mapping-property-inactive");
        Assert.Single(result.Value.RepairPreview.InactiveResourcePropertyAssignments);
        Assert.Single(result.Value.RepairPreview.InactiveResourceTypePropertyMappings);
    }

    [Fact]
    public async Task Cleanup_Removes_Only_Inactive_Property_References_And_Leaves_Diagnostics_For_Other_Legacy_Cases()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedLegacyTenantAsync(provider);

        var result = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<ILegacyConsistencyService>();
            return await service.CleanupInactivePropertyReferencesAsync(CancellationToken.None);
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value!.RemovedResourcePropertyAssignments);
        Assert.Equal(1, result.Value.RemovedResourceTypePropertyMappings);
        Assert.DoesNotContain(result.Value.ReportAfter.Validation.Findings, finding => finding.Code == "validation.resource.assigned-property-inactive");
        Assert.DoesNotContain(result.Value.ReportAfter.Validation.Findings, finding => finding.Code == "validation.resource-type.mapping-property-inactive");
        Assert.Contains(result.Value.ReportAfter.Validation.Findings, finding => finding.Code == "validation.rule.resource-inactive");
        Assert.Contains(result.Value.ReportAfter.Validation.Findings, finding => finding.Code == "validation.busy-event.resource-inactive");
        Assert.Empty(result.Value.ReportAfter.RepairPreview.InactiveResourcePropertyAssignments);
        Assert.Empty(result.Value.ReportAfter.RepairPreview.InactiveResourceTypePropertyMappings);
    }

    [Fact]
    public async Task Legacy_Consistency_Requires_Tenant_Context()
    {
        await using var provider = BuildServiceProvider();

        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILegacyConsistencyService>();

        var report = await service.GetLegacyConsistencyReportAsync(CancellationToken.None);
        var cleanup = await service.CleanupInactivePropertyReferencesAsync(CancellationToken.None);

        Assert.False(report.Succeeded);
        Assert.Equal("tenant.context.unresolved", report.Errors[0].Code);
        Assert.False(cleanup.Succeeded);
        Assert.Equal("tenant.context.unresolved", cleanup.Errors[0].Code);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = $"legacy-consistency-{Guid.NewGuid()}";

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddDbContext<SchedulerDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IPropertySchemaQueryService, PropertySchemaQueryService>();
        services.AddScoped<IPropertySchemaService, PropertySchemaService>();
        services.AddScoped<IManagementValidationStore, ManagementValidationStore>();
        services.AddScoped<IManagementValidationService, ManagementValidationService>();
        services.AddScoped<ILegacyConsistencyService, LegacyConsistencyService>();

        return services.BuildServiceProvider();
    }

    private static async Task<TenantInfo> SeedLegacyTenantAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var tenantId = new Guid("99999999-9999-9999-9999-999999999999");
        tenantContext.SetTenant(tenantId, "default");

        dbContext.Tenants.Add(new Tenants
        {
            Id = tenantId,
            Key = "default",
            Label = "Default",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 22, 10, 0, 0, DateTimeKind.Utc)
        });

        dbContext.ResourceTypes.Add(new ResourceTypes { Id = 1, TenantId = tenantId, Key = "room", Label = "Room", SortOrder = 1, IsActive = true });
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
                CreatedAtUtc = new DateTime(2026, 3, 22, 10, 0, 0, DateTimeKind.Utc)
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
                IsActive = false,
                IsArchived = true,
                CreatedAtUtc = new DateTime(2026, 3, 22, 10, 5, 0, DateTimeKind.Utc)
            });

        dbContext.ResourceProperties.AddRange(
            new ResourceProperties { Id = 100, TenantId = tenantId, Key = "active", Label = "Active", SortOrder = 1, IsActive = true },
            new ResourceProperties { Id = 200, TenantId = tenantId, Key = "legacy", Label = "Legacy", SortOrder = 2, IsActive = false });

        dbContext.ResourcePropertyLinks.Add(new ResourcePropertyLinks
        {
            TenantId = tenantId,
            ResourceId = 1,
            PropertyId = 200
        });

        dbContext.ResourceTypeProperties.Add(new ResourceTypeProperties
        {
            TenantId = tenantId,
            ResourceTypeId = 1,
            PropertyDefinitionId = 200
        });

        dbContext.Rules.Add(new Rules
        {
            Id = 500,
            TenantId = tenantId,
            Kind = (byte)HelixScheduler.Core.RuleKind.SingleDate,
            IsExclude = false,
            SingleDateUtc = new DateOnly(2026, 3, 25),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 22, 10, 0, 0, DateTimeKind.Utc)
        });
        dbContext.RuleResources.Add(new RuleResources
        {
            TenantId = tenantId,
            RuleId = 500,
            ResourceId = 2
        });

        dbContext.BusyEvents.Add(new BusyEvents
        {
            Id = 600,
            TenantId = tenantId,
            StartUtc = new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc),
            EndUtc = new DateTime(2026, 3, 25, 13, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 22, 10, 0, 0, DateTimeKind.Utc)
        });
        dbContext.BusyEventResources.Add(new BusyEventResources
        {
            TenantId = tenantId,
            BusyEventId = 600,
            ResourceId = 2
        });

        await dbContext.SaveChangesAsync(CancellationToken.None);
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
