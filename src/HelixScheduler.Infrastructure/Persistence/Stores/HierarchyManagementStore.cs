using HelixScheduler.Application.Hierarchy;
using HelixScheduler.Application.Abstractions;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Stores;

public sealed class HierarchyManagementStore : IHierarchyManagementStore
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public HierarchyManagementStore(
        SchedulerDbContext dbContext,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<HierarchyRelationDto>> ListRelationsAsync(CancellationToken ct)
    {
        return await _dbContext.ResourceRelations
            .AsNoTracking()
            .OrderBy(relation => relation.ChildResourceId)
            .ThenBy(relation => relation.ParentResourceId)
            .ThenBy(relation => relation.RelationType)
            .Select(relation => new HierarchyRelationDto(
                relation.ParentResourceId,
                relation.ChildResourceId,
                relation.RelationType))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public Task<bool> RelationExistsAsync(int parentResourceId, int childResourceId, string relationType, CancellationToken ct)
    {
        return _dbContext.ResourceRelations.AnyAsync(
            relation => relation.ParentResourceId == parentResourceId
                     && relation.ChildResourceId == childResourceId
                     && relation.RelationType == relationType,
            ct);
    }

    public async Task<HierarchyResourceState?> FindResourceStateAsync(int resourceId, CancellationToken ct)
    {
        return await _dbContext.Resources
            .AsNoTracking()
            .Where(resource => resource.Id == resourceId)
            .Select(resource => new HierarchyResourceState(
                resource.Id,
                resource.IsActive,
                resource.IsArchived))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HierarchyRelationDto>> GetRelationsByRelationTypeAsync(string relationType, CancellationToken ct)
    {
        return await _dbContext.ResourceRelations
            .AsNoTracking()
            .Where(relation => relation.RelationType == relationType)
            .Select(relation => new HierarchyRelationDto(
                relation.ParentResourceId,
                relation.ChildResourceId,
                relation.RelationType))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<HierarchyRelationDto> AddRelationAsync(int parentResourceId, int childResourceId, string relationType, CancellationToken ct)
    {
        var entity = new ResourceRelations
        {
            TenantId = _tenantContext.TenantId,
            ParentResourceId = parentResourceId,
            ChildResourceId = childResourceId,
            RelationType = relationType
        };

        _dbContext.ResourceRelations.Add(entity);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        return new HierarchyRelationDto(entity.ParentResourceId, entity.ChildResourceId, entity.RelationType);
    }

    public async Task<HierarchyRelationDto?> RemoveRelationAsync(int parentResourceId, int childResourceId, string relationType, CancellationToken ct)
    {
        var entity = await _dbContext.ResourceRelations
            .FirstOrDefaultAsync(
                relation => relation.ParentResourceId == parentResourceId
                         && relation.ChildResourceId == childResourceId
                         && relation.RelationType == relationType,
                ct)
            .ConfigureAwait(false);

        if (entity == null)
        {
            return null;
        }

        _dbContext.ResourceRelations.Remove(entity);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        return new HierarchyRelationDto(entity.ParentResourceId, entity.ChildResourceId, entity.RelationType);
    }
}
