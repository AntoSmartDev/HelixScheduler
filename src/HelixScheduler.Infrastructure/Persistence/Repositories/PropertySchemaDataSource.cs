using HelixScheduler.Application.PropertySchema;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class PropertySchemaDataSource : IPropertySchemaDataSource
{
    private readonly SchedulerDbContext _dbContext;

    public PropertySchemaDataSource(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PropertySchemaNode>> GetPropertyNodesAsync(CancellationToken ct)
    {
        return await _dbContext.ResourceProperties
            .AsNoTracking()
            .OrderBy(property => property.Key)
            .ThenBy(property => property.SortOrder)
            .ThenBy(property => property.Label)
            .Select(property => new PropertySchemaNode(
                property.Id,
                property.ParentId,
                property.Key,
                property.Label,
                property.SortOrder))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ResourceTypePropertyLink>> GetResourceTypePropertiesAsync(CancellationToken ct)
    {
        return await _dbContext.ResourceTypeProperties
            .AsNoTracking()
            .Select(link => new ResourceTypePropertyLink(
                link.ResourceTypeId,
                link.PropertyDefinitionId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ResourceTypeAssignment>> GetResourceTypeAssignmentsAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<ResourceTypeAssignment>();
        }

        return await _dbContext.Resources
            .AsNoTracking()
            .Where(resource => resourceIds.Contains(resource.Id))
            .Select(resource => new ResourceTypeAssignment(resource.Id, resource.TypeId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
