using HelixScheduler.Application.PropertyManagement;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Stores;

public sealed class ResourcePropertyAssignmentManagementStore : IResourcePropertyAssignmentManagementStore
{
    private readonly SchedulerDbContext _dbContext;

    public ResourcePropertyAssignmentManagementStore(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> AssignmentExistsAsync(int resourceId, int propertyId, CancellationToken ct)
    {
        return _dbContext.ResourcePropertyLinks.AnyAsync(
            link => link.ResourceId == resourceId && link.PropertyId == propertyId,
            ct);
    }

    public async Task<IReadOnlyList<PropertyManagementDto>> ListAssignedPropertiesAsync(int resourceId, CancellationToken ct)
    {
        return await _dbContext.ResourcePropertyLinks
            .AsNoTracking()
            .Where(link => link.ResourceId == resourceId)
            .OrderBy(link => link.Property.Key)
            .ThenBy(link => link.Property.SortOrder)
            .ThenBy(link => link.Property.Label)
            .Select(link => new PropertyManagementDto(
                link.Property.Id,
                link.Property.Key,
                link.Property.Label,
                link.Property.ParentId,
                link.Property.SortOrder,
                link.Property.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAssignmentsAsync(Guid tenantId, int resourceId, IReadOnlyList<int> propertyIds, CancellationToken ct)
    {
        for (var i = 0; i < propertyIds.Count; i++)
        {
            _dbContext.ResourcePropertyLinks.Add(new ResourcePropertyLinks
            {
                TenantId = tenantId,
                ResourceId = resourceId,
                PropertyId = propertyIds[i]
            });
        }

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveAssignmentsAsync(int resourceId, IReadOnlyList<int> propertyIds, CancellationToken ct)
    {
        var links = await _dbContext.ResourcePropertyLinks
            .Where(link => link.ResourceId == resourceId && propertyIds.Contains(link.PropertyId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _dbContext.ResourcePropertyLinks.RemoveRange(links);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
