using HelixScheduler.Application.ResourceCatalog;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.QueryServices;

public sealed class ResourceTypeCatalogQueryService : IResourceTypeCatalogQueryService
{
    private readonly SchedulerDbContext _dbContext;

    public ResourceTypeCatalogQueryService(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ResourceTypeDto>> GetResourceTypesAsync(CancellationToken ct)
    {
        return await _dbContext.ResourceTypes
            .AsNoTracking()
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.Label)
            .Select(type => new ResourceTypeDto(
                type.Id,
                type.Key,
                type.Label,
                type.SortOrder))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
