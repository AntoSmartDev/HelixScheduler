using HelixScheduler.Application.ResourceCatalog.Management;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Stores;

public sealed class ResourceManagementStore : IResourceManagementStore
{
    private readonly SchedulerDbContext _dbContext;

    public ResourceManagementStore(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResourceManagementDto?> FindByIdAsync(int resourceId, CancellationToken ct)
    {
        var entity = await _dbContext.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(resource => resource.Id == resourceId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<ResourceManagementDto>> ListAsync(CancellationToken ct)
    {
        return await _dbContext.Resources
            .AsNoTracking()
            .Where(resource => !resource.IsArchived)
            .OrderBy(resource => resource.Name)
            .Select(resource => new ResourceManagementDto(
                resource.Id,
                resource.Code,
                resource.Name,
                resource.IsSchedulable,
                resource.Capacity,
                resource.TypeId,
                resource.IsActive,
                resource.IsArchived,
                resource.CreatedAtUtc))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<ResourceManagementDto> CreateAsync(
        Guid tenantId,
        string? code,
        string name,
        bool isSchedulable,
        int capacity,
        int typeId,
        DateTime createdAtUtc,
        CancellationToken ct)
    {
        var entity = new Resources
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            IsSchedulable = isSchedulable,
            Capacity = capacity,
            TypeId = typeId,
            IsActive = true,
            IsArchived = false,
            CreatedAtUtc = createdAtUtc
        };

        _dbContext.Resources.Add(entity);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<ResourceManagementDto> UpdateAsync(
        int resourceId,
        string? code,
        string name,
        bool isSchedulable,
        int capacity,
        int typeId,
        CancellationToken ct)
    {
        var entity = await _dbContext.Resources
            .FirstAsync(resource => resource.Id == resourceId, ct)
            .ConfigureAwait(false);

        entity.Code = code;
        entity.Name = name;
        entity.IsSchedulable = isSchedulable;
        entity.Capacity = capacity;
        entity.TypeId = typeId;

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<ResourceManagementDto> SetActiveAsync(int resourceId, bool isActive, CancellationToken ct)
    {
        var entity = await _dbContext.Resources
            .FirstAsync(resource => resource.Id == resourceId, ct)
            .ConfigureAwait(false);

        entity.IsActive = isActive;
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<ResourceManagementDto> ArchiveAsync(int resourceId, CancellationToken ct)
    {
        var entity = await _dbContext.Resources
            .FirstAsync(resource => resource.Id == resourceId, ct)
            .ConfigureAwait(false);

        entity.IsArchived = true;
        entity.IsActive = false;
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    private static ResourceManagementDto Map(Resources entity)
    {
        return new ResourceManagementDto(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.IsSchedulable,
            entity.Capacity,
            entity.TypeId,
            entity.IsActive,
            entity.IsArchived,
            entity.CreatedAtUtc);
    }
}
