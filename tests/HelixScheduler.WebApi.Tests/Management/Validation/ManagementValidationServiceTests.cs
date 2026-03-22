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

public sealed class ManagementValidationServiceTests
{
    [Fact]
    public async Task ValidateTenantModel_Returns_Explainable_Findings_For_Legacy_Inconsistencies()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedInconsistentTenantAsync(provider);

        var result = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IManagementValidationService>();
            return await service.ValidateTenantModelAsync(CancellationToken.None);
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Findings, finding => finding.Code == "validation.resource.type-inactive");
        Assert.Contains(result.Findings, finding => finding.Code == "validation.hierarchy.cycle-detected");
        Assert.Contains(result.Findings, finding => finding.Code == "validation.property.active-child-inactive-parent");
        Assert.Contains(result.Findings, finding => finding.Code == "validation.rule.resource-inactive");
        Assert.Contains(result.Findings, finding => finding.Code == "validation.busy-event.resource-inactive");
        Assert.Contains(result.Findings, finding => finding.Code == "validation.resource.property-type-incompatibility");
    }

    [Fact]
    public async Task ValidateResourceConfiguration_Returns_Targeted_Findings()
    {
        await using var provider = BuildServiceProvider();
        var tenant = await SeedInconsistentTenantAsync(provider);

        var resourceOne = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IManagementValidationService>();
            return await service.ValidateResourceConfigurationAsync(1, CancellationToken.None);
        });

        Assert.False(resourceOne.IsValid);
        Assert.Contains(resourceOne.Findings, finding => finding.Code == "validation.resource.property-type-incompatibility");

        var resourceThree = await ExecuteForTenantAsync(provider, tenant, async sp =>
        {
            var service = sp.GetRequiredService<IManagementValidationService>();
            return await service.ValidateResourceConfigurationAsync(3, CancellationToken.None);
        });

        Assert.False(resourceThree.IsValid);
        Assert.Contains(resourceThree.Findings, finding => finding.Code == "validation.resource.active-rule-on-inactive-resource");
        Assert.Contains(resourceThree.Findings, finding => finding.Code == "validation.resource.active-busy-on-inactive-resource");
    }

    [Fact]
    public async Task Validation_Requires_Tenant_Context()
    {
        await using var provider = BuildServiceProvider();

        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IManagementValidationService>();

        var tenantResult = await service.ValidateTenantModelAsync(CancellationToken.None);
        var resourceResult = await service.ValidateResourceConfigurationAsync(1, CancellationToken.None);

        Assert.False(tenantResult.IsValid);
        Assert.Equal("tenant.context.unresolved", tenantResult.Findings[0].Code);
        Assert.False(resourceResult.IsValid);
        Assert.Equal("tenant.context.unresolved", resourceResult.Findings[0].Code);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = $"management-validation-{Guid.NewGuid()}";

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddDbContext<SchedulerDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IPropertySchemaQueryService, PropertySchemaQueryService>();
        services.AddScoped<IPropertySchemaService, PropertySchemaService>();
        services.AddScoped<IManagementValidationStore, ManagementValidationStore>();
        services.AddScoped<IManagementValidationService, ManagementValidationService>();

        return services.BuildServiceProvider();
    }

    private static async Task<TenantInfo> SeedInconsistentTenantAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var tenantId = new Guid("88888888-8888-8888-8888-888888888888");
        tenantContext.SetTenant(tenantId, "default");

        dbContext.Tenants.Add(new Tenants
        {
            Id = tenantId,
            Key = "default",
            Label = "Default",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 21, 15, 0, 0, DateTimeKind.Utc)
        });

        dbContext.ResourceTypes.AddRange(
            new ResourceTypes { Id = 1, TenantId = tenantId, Key = "room", Label = "Room", SortOrder = 1, IsActive = true },
            new ResourceTypes { Id = 2, TenantId = tenantId, Key = "device", Label = "Device", SortOrder = 2, IsActive = false });

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
                CreatedAtUtc = new DateTime(2026, 3, 21, 15, 0, 0, DateTimeKind.Utc)
            },
            new Resources
            {
                Id = 2,
                TenantId = tenantId,
                Code = "R2",
                Name = "Room 2",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 2,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = new DateTime(2026, 3, 21, 15, 0, 0, DateTimeKind.Utc)
            },
            new Resources
            {
                Id = 3,
                TenantId = tenantId,
                Code = "R3",
                Name = "Room 3",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                IsActive = false,
                IsArchived = true,
                CreatedAtUtc = new DateTime(2026, 3, 21, 15, 0, 0, DateTimeKind.Utc)
            });

        dbContext.ResourceRelations.AddRange(
            new ResourceRelations { TenantId = tenantId, ParentResourceId = 1, ChildResourceId = 2, RelationType = "Contains" },
            new ResourceRelations { TenantId = tenantId, ParentResourceId = 2, ChildResourceId = 1, RelationType = "Contains" });

        dbContext.ResourceProperties.AddRange(
            new ResourceProperties { Id = 100, TenantId = tenantId, Key = "Capability", Label = "Capability", ParentId = null, SortOrder = 1, IsActive = true },
            new ResourceProperties { Id = 101, TenantId = tenantId, Key = "Capability", Label = "ChildCapability", ParentId = 200, SortOrder = 2, IsActive = true },
            new ResourceProperties { Id = 200, TenantId = tenantId, Key = "Legacy", Label = "LegacyRoot", ParentId = null, SortOrder = 3, IsActive = false },
            new ResourceProperties { Id = 300, TenantId = tenantId, Key = "Other", Label = "Other", ParentId = null, SortOrder = 4, IsActive = true });

        dbContext.ResourceTypeProperties.Add(new ResourceTypeProperties
        {
            TenantId = tenantId,
            ResourceTypeId = 1,
            PropertyDefinitionId = 100
        });

        dbContext.ResourcePropertyLinks.AddRange(
            new ResourcePropertyLinks { TenantId = tenantId, ResourceId = 1, PropertyId = 300 },
            new ResourcePropertyLinks { TenantId = tenantId, ResourceId = 3, PropertyId = 200 });

        dbContext.Rules.Add(new Rules
        {
            Id = 500,
            TenantId = tenantId,
            Kind = 2,
            IsExclude = false,
            SingleDateUtc = new DateOnly(2026, 3, 25),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 21, 15, 0, 0, DateTimeKind.Utc)
        });
        dbContext.RuleResources.Add(new RuleResources
        {
            TenantId = tenantId,
            RuleId = 500,
            ResourceId = 3
        });

        dbContext.BusyEvents.Add(new BusyEvents
        {
            Id = 600,
            TenantId = tenantId,
            StartUtc = new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc),
            EndUtc = new DateTime(2026, 3, 25, 13, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 3, 21, 15, 0, 0, DateTimeKind.Utc)
        });
        dbContext.BusyEventResources.Add(new BusyEventResources
        {
            TenantId = tenantId,
            BusyEventId = 600,
            ResourceId = 3
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
