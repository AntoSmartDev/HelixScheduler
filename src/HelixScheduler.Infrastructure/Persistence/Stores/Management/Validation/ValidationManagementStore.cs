using HelixScheduler.Application.Management.Validation;
using HelixScheduler.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Stores.Management.Validation;

public sealed class ManagementValidationStore : IManagementValidationStore
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public ManagementValidationStore(
        SchedulerDbContext dbContext,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<TenantValidationSnapshot> LoadTenantSnapshotAsync(CancellationToken ct)
    {
        var resources = await _dbContext.Resources
            .AsNoTracking()
            .Select(resource => new ResourceValidationSnapshot(
                resource.Id,
                resource.TypeId,
                resource.IsActive,
                resource.IsArchived))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var resourceTypes = await _dbContext.ResourceTypes
            .AsNoTracking()
            .Select(type => new ResourceTypeValidationSnapshot(
                type.Id,
                type.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var resourceRelations = await _dbContext.ResourceRelations
            .AsNoTracking()
            .Select(relation => new ResourceRelationValidationSnapshot(
                relation.ParentResourceId,
                relation.ChildResourceId,
                relation.RelationType))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var properties = await _dbContext.ResourceProperties
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(property => property.TenantId == _tenantContext.TenantId)
            .Select(property => new PropertyValidationSnapshot(
                property.Id,
                property.ParentId,
                property.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var resourcePropertyAssignments = await _dbContext.ResourcePropertyLinks
            .AsNoTracking()
            .Select(link => new ResourcePropertyAssignmentSnapshot(
                link.ResourceId,
                link.PropertyId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var typeMappings = await _dbContext.ResourceTypeProperties
            .AsNoTracking()
            .Select(link => new ResourceTypePropertyMappingSnapshot(
                link.ResourceTypeId,
                link.PropertyDefinitionId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ruleBindings = await _dbContext.RuleResources
            .AsNoTracking()
            .Select(link => new RuleResourceBindingSnapshot(
                link.RuleId,
                link.ResourceId,
                link.Rule.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var busyBindings = await _dbContext.BusyEventResources
            .AsNoTracking()
            .Select(link => new BusyEventResourceBindingSnapshot(
                link.BusyEventId,
                link.ResourceId,
                link.BusyEvent.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new TenantValidationSnapshot(
            resources,
            resourceTypes,
            resourceRelations,
            properties,
            resourcePropertyAssignments,
            typeMappings,
            ruleBindings,
            busyBindings);
    }

    public async Task<LegacyPropertyReferenceSnapshot> LoadInactivePropertyReferenceSnapshotAsync(CancellationToken ct)
    {
        var inactiveAssignments = await _dbContext.ResourcePropertyLinks
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(link => link.TenantId == _tenantContext.TenantId && !link.Property.IsActive)
            .Select(link => new InactiveResourcePropertyAssignmentReference(
                link.ResourceId,
                link.PropertyId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var inactiveMappings = await _dbContext.ResourceTypeProperties
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(link => link.TenantId == _tenantContext.TenantId && !link.PropertyDefinition.IsActive)
            .Select(link => new InactiveResourceTypePropertyMappingReference(
                link.ResourceTypeId,
                link.PropertyDefinitionId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new LegacyPropertyReferenceSnapshot(inactiveAssignments, inactiveMappings);
    }

    public async Task<LegacyPropertyReferenceCleanupResult> RemoveInactivePropertyReferencesAsync(CancellationToken ct)
    {
        var assignments = await _dbContext.ResourcePropertyLinks
            .IgnoreQueryFilters()
            .Where(link => link.TenantId == _tenantContext.TenantId && !link.Property.IsActive)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var mappings = await _dbContext.ResourceTypeProperties
            .IgnoreQueryFilters()
            .Where(link => link.TenantId == _tenantContext.TenantId && !link.PropertyDefinition.IsActive)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (assignments.Count > 0)
        {
            _dbContext.ResourcePropertyLinks.RemoveRange(assignments);
        }

        if (mappings.Count > 0)
        {
            _dbContext.ResourceTypeProperties.RemoveRange(mappings);
        }

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        return new LegacyPropertyReferenceCleanupResult(assignments.Count, mappings.Count);
    }
}
