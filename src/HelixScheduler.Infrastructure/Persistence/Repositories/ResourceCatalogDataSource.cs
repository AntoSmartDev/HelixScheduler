using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class ResourceCatalogDataSource : IResourceCatalogDataSource
{
    private readonly SchedulerDbContext _dbContext;

    public ResourceCatalogDataSource(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ResourceCatalogResource>> GetResourcesAsync(
        bool onlySchedulable,
        CancellationToken ct)
    {
        IQueryable<Resources> query = _dbContext.Resources
            .AsNoTracking()
            .Include(resource => resource.Type);
        if (onlySchedulable)
        {
            query = query.Where(resource => resource.IsSchedulable);
        }

        return await query
            .OrderBy(resource => resource.Name)
            .Select(resource => new ResourceCatalogResource(
                resource.Id,
                resource.Code,
                resource.Name,
                resource.IsSchedulable,
                resource.TypeId,
                resource.Type.Key,
                resource.Type.Label))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ResourceCatalogProperty>> GetPropertiesAsync(CancellationToken ct)
    {
        return await _dbContext.ResourceProperties
            .AsNoTracking()
            .OrderBy(property => property.Key)
            .ThenBy(property => property.SortOrder)
            .ThenBy(property => property.Label)
            .Select(property => new ResourceCatalogProperty(
                property.Id,
                property.Key,
                property.Label,
                property.ParentId,
                property.SortOrder))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ResourcePropertyLink>> GetPropertyLinksAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<ResourcePropertyLink>();
        }

        return await _dbContext.ResourcePropertyLinks
            .AsNoTracking()
            .Where(link => resourceIds.Contains(link.ResourceId))
            .Select(link => new ResourcePropertyLink(
                link.ResourceId,
                link.PropertyId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
