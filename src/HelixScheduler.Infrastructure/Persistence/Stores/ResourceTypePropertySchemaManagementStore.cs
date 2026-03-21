using HelixScheduler.Application.ResourceCatalog.Management;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Stores;

public sealed class ResourceTypePropertySchemaManagementStore : IResourceTypePropertySchemaManagementStore
{
    private readonly SchedulerDbContext _dbContext;

    public ResourceTypePropertySchemaManagementStore(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PropertyDefinitionManagementState>> GetPropertyDefinitionsAsync(
        IReadOnlyList<int> propertyDefinitionIds,
        CancellationToken ct)
    {
        if (propertyDefinitionIds.Count == 0)
        {
            return Array.Empty<PropertyDefinitionManagementState>();
        }

        return await _dbContext.ResourceProperties
            .AsNoTracking()
            .Where(property => propertyDefinitionIds.Contains(property.Id))
            .Select(property => new PropertyDefinitionManagementState(property.Id, property.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<int>> ListAssignedPropertyDefinitionIdsAsync(
        int resourceTypeId,
        CancellationToken ct)
    {
        return await _dbContext.ResourceTypeProperties
            .AsNoTracking()
            .Where(link => link.ResourceTypeId == resourceTypeId)
            .OrderBy(link => link.PropertyDefinitionId)
            .Select(link => link.PropertyDefinitionId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAssignmentsAsync(
        Guid tenantId,
        int resourceTypeId,
        IReadOnlyList<int> propertyDefinitionIds,
        CancellationToken ct)
    {
        foreach (var propertyDefinitionId in propertyDefinitionIds)
        {
            _dbContext.ResourceTypeProperties.Add(new ResourceTypeProperties
            {
                TenantId = tenantId,
                ResourceTypeId = resourceTypeId,
                PropertyDefinitionId = propertyDefinitionId
            });
        }

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveAssignmentsAsync(
        int resourceTypeId,
        IReadOnlyList<int> propertyDefinitionIds,
        CancellationToken ct)
    {
        var entities = await _dbContext.ResourceTypeProperties
            .Where(link => link.ResourceTypeId == resourceTypeId && propertyDefinitionIds.Contains(link.PropertyDefinitionId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _dbContext.ResourceTypeProperties.RemoveRange(entities);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
