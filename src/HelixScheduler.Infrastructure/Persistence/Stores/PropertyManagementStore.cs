using HelixScheduler.Application.PropertyManagement;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Stores;

public sealed class PropertyManagementStore : IPropertyManagementStore
{
    private readonly SchedulerDbContext _dbContext;

    public PropertyManagementStore(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PropertyManagementDto?> FindByIdAsync(int propertyId, CancellationToken ct)
    {
        var entity = await _dbContext.ResourceProperties
            .AsNoTracking()
            .FirstOrDefaultAsync(property => property.Id == propertyId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : Map(entity);
    }

    public async Task<PropertyManagementDto?> FindByKeyAsync(string key, CancellationToken ct)
    {
        var entity = await _dbContext.ResourceProperties
            .AsNoTracking()
            .FirstOrDefaultAsync(property => property.Key == key, ct)
            .ConfigureAwait(false);

        return entity == null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<PropertyManagementDto>> FindByIdsAsync(IReadOnlyList<int> propertyIds, CancellationToken ct)
    {
        if (propertyIds.Count == 0)
        {
            return Array.Empty<PropertyManagementDto>();
        }

        return await _dbContext.ResourceProperties
            .AsNoTracking()
            .Where(property => propertyIds.Contains(property.Id))
            .OrderBy(property => property.Key)
            .ThenBy(property => property.SortOrder)
            .ThenBy(property => property.Label)
            .Select(property => new PropertyManagementDto(
                property.Id,
                property.Key,
                property.Label,
                property.ParentId,
                property.SortOrder,
                property.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PropertyManagementDto>> ListAsync(CancellationToken ct)
    {
        return await _dbContext.ResourceProperties
            .AsNoTracking()
            .OrderBy(property => property.Key)
            .ThenBy(property => property.SortOrder)
            .ThenBy(property => property.Label)
            .Select(property => new PropertyManagementDto(
                property.Id,
                property.Key,
                property.Label,
                property.ParentId,
                property.SortOrder,
                property.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<PropertyManagementDto> CreateAsync(
        Guid tenantId,
        string key,
        string label,
        int? sortOrder,
        CancellationToken ct)
    {
        var entity = new ResourceProperties
        {
            TenantId = tenantId,
            Key = key,
            Label = label,
            SortOrder = sortOrder,
            IsActive = true
        };

        _dbContext.ResourceProperties.Add(entity);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<PropertyManagementDto> UpdateAsync(
        int propertyId,
        string key,
        string label,
        int? sortOrder,
        CancellationToken ct)
    {
        var entity = await _dbContext.ResourceProperties
            .FirstAsync(property => property.Id == propertyId, ct)
            .ConfigureAwait(false);

        entity.Key = key;
        entity.Label = label;
        entity.SortOrder = sortOrder;

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<PropertyManagementDto> SetActiveAsync(int propertyId, bool isActive, CancellationToken ct)
    {
        var entity = await _dbContext.ResourceProperties
            .FirstAsync(property => property.Id == propertyId, ct)
            .ConfigureAwait(false);

        entity.IsActive = isActive;
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public Task<bool> HasChildPropertiesAsync(int propertyId, CancellationToken ct)
    {
        return _dbContext.ResourceProperties.AnyAsync(property => property.ParentId == propertyId, ct);
    }

    public Task<bool> HasResourceAssignmentsAsync(int propertyId, CancellationToken ct)
    {
        return _dbContext.ResourcePropertyLinks.AnyAsync(link => link.PropertyId == propertyId, ct);
    }

    public Task<bool> HasTypeMappingsAsync(int propertyId, CancellationToken ct)
    {
        return _dbContext.ResourceTypeProperties.AnyAsync(link => link.PropertyDefinitionId == propertyId, ct);
    }

    public async Task<IReadOnlyList<PropertyHierarchyRelationDto>> ListRelationsAsync(CancellationToken ct)
    {
        return await _dbContext.ResourceProperties
            .AsNoTracking()
            .Where(property => property.ParentId != null)
            .OrderBy(property => property.ParentId)
            .ThenBy(property => property.Id)
            .Select(property => new PropertyHierarchyRelationDto(
                property.ParentId!.Value,
                property.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public Task<bool> RelationExistsAsync(int parentPropertyId, int childPropertyId, CancellationToken ct)
    {
        return _dbContext.ResourceProperties.AnyAsync(
            property => property.Id == childPropertyId && property.ParentId == parentPropertyId,
            ct);
    }

    public Task<IReadOnlyList<PropertyHierarchyRelationDto>> GetRelationsAsync(CancellationToken ct)
    {
        return ListRelationsAsync(ct);
    }

    public async Task<PropertyHierarchyRelationDto> AddParentRelationAsync(int parentPropertyId, int childPropertyId, CancellationToken ct)
    {
        var child = await _dbContext.ResourceProperties
            .FirstAsync(property => property.Id == childPropertyId, ct)
            .ConfigureAwait(false);

        child.ParentId = parentPropertyId;
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return new PropertyHierarchyRelationDto(parentPropertyId, childPropertyId);
    }

    public async Task<PropertyHierarchyRelationDto?> RemoveParentRelationAsync(int parentPropertyId, int childPropertyId, CancellationToken ct)
    {
        var child = await _dbContext.ResourceProperties
            .FirstOrDefaultAsync(
                property => property.Id == childPropertyId && property.ParentId == parentPropertyId,
                ct)
            .ConfigureAwait(false);

        if (child == null)
        {
            return null;
        }

        child.ParentId = null;
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return new PropertyHierarchyRelationDto(parentPropertyId, childPropertyId);
    }

    private static PropertyManagementDto Map(ResourceProperties entity)
    {
        return new PropertyManagementDto(
            entity.Id,
            entity.Key,
            entity.Label,
            entity.ParentId,
            entity.SortOrder,
            entity.IsActive);
    }
}
