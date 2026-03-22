using HelixScheduler.Application.Management.ResourceCatalog;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Stores;

public sealed class ResourceTypeManagementStore : IResourceTypeManagementStore
{
    private readonly SchedulerDbContext _dbContext;

    public ResourceTypeManagementStore(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResourceTypeManagementDto?> FindByIdAsync(int resourceTypeId, CancellationToken ct)
    {
        var entity = await _dbContext.ResourceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.Id == resourceTypeId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : Map(entity);
    }

    public async Task<ResourceTypeManagementDto?> FindByKeyAsync(string key, CancellationToken ct)
    {
        var entity = await _dbContext.ResourceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.Key == key, ct)
            .ConfigureAwait(false);

        return entity == null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<ResourceTypeManagementDto>> ListAsync(CancellationToken ct)
    {
        return await _dbContext.ResourceTypes
            .AsNoTracking()
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.Label)
            .Select(type => new ResourceTypeManagementDto(
                type.Id,
                type.Key,
                type.Label,
                type.SortOrder,
                type.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<ResourceTypeManagementDto> CreateAsync(Guid tenantId, string key, string label, int? sortOrder, CancellationToken ct)
    {
        var entity = new ResourceTypes
        {
            TenantId = tenantId,
            Key = key,
            Label = label,
            SortOrder = sortOrder,
            IsActive = true
        };

        _dbContext.ResourceTypes.Add(entity);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<ResourceTypeManagementDto> UpdateAsync(int resourceTypeId, string key, string label, int? sortOrder, CancellationToken ct)
    {
        var entity = await _dbContext.ResourceTypes
            .FirstAsync(type => type.Id == resourceTypeId, ct)
            .ConfigureAwait(false);

        entity.Key = key;
        entity.Label = label;
        entity.SortOrder = sortOrder;

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<ResourceTypeManagementDto> SetActiveAsync(int resourceTypeId, bool isActive, CancellationToken ct)
    {
        var entity = await _dbContext.ResourceTypes
            .FirstAsync(type => type.Id == resourceTypeId, ct)
            .ConfigureAwait(false);

        entity.IsActive = isActive;
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public Task<bool> HasActiveResourcesAsync(int resourceTypeId, CancellationToken ct)
    {
        return _dbContext.Resources.AnyAsync(
            resource => resource.TypeId == resourceTypeId && resource.IsActive && !resource.IsArchived,
            ct);
    }

    private static ResourceTypeManagementDto Map(ResourceTypes entity)
    {
        return new ResourceTypeManagementDto(
            entity.Id,
            entity.Key,
            entity.Label,
            entity.SortOrder,
            entity.IsActive);
    }
}
