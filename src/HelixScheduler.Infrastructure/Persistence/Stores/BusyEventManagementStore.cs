using HelixScheduler.Application.BusyEventManagement;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Stores;

public sealed class BusyEventManagementStore : IBusyEventManagementStore
{
    private readonly SchedulerDbContext _dbContext;

    public BusyEventManagementStore(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BusyEventManagementDto?> FindByIdAsync(long busyEventId, CancellationToken ct)
    {
        var entity = await _dbContext.BusyEvents
            .AsNoTracking()
            .Include(busyEvent => busyEvent.BusyEventResources)
            .FirstOrDefaultAsync(busyEvent => busyEvent.Id == busyEventId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<BusyEventManagementDto>> ListAsync(CancellationToken ct)
    {
        var entities = await _dbContext.BusyEvents
            .AsNoTracking()
            .Include(busyEvent => busyEvent.BusyEventResources)
            .OrderByDescending(busyEvent => busyEvent.StartUtc)
            .ThenByDescending(busyEvent => busyEvent.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(Map).ToList();
    }

    public async Task<BusyEventManagementDto> CreateAsync(
        Guid tenantId,
        BusyEventDefinition definition,
        DateTime createdAtUtc,
        CancellationToken ct)
    {
        var entity = new BusyEvents
        {
            TenantId = tenantId,
            Title = definition.Title,
            StartUtc = definition.StartUtc,
            EndUtc = definition.EndUtc,
            EventType = definition.EventType,
            IsActive = true,
            CreatedAtUtc = createdAtUtc
        };

        for (var i = 0; i < definition.ResourceIds.Count; i++)
        {
            entity.BusyEventResources.Add(new BusyEventResources
            {
                TenantId = tenantId,
                ResourceId = definition.ResourceIds[i]
            });
        }

        _dbContext.BusyEvents.Add(entity);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<BusyEventManagementDto> UpdateAsync(long busyEventId, BusyEventDefinition definition, CancellationToken ct)
    {
        var entity = await _dbContext.BusyEvents
            .Include(busyEvent => busyEvent.BusyEventResources)
            .FirstAsync(busyEvent => busyEvent.Id == busyEventId, ct)
            .ConfigureAwait(false);

        entity.Title = definition.Title;
        entity.StartUtc = definition.StartUtc;
        entity.EndUtc = definition.EndUtc;
        entity.EventType = definition.EventType;

        var existingResourceIds = entity.BusyEventResources.Select(link => link.ResourceId).ToHashSet();
        var targetResourceIds = definition.ResourceIds.ToHashSet();

        var toRemove = entity.BusyEventResources.Where(link => !targetResourceIds.Contains(link.ResourceId)).ToList();
        if (toRemove.Count > 0)
        {
            _dbContext.BusyEventResources.RemoveRange(toRemove);
        }

        foreach (var resourceId in definition.ResourceIds)
        {
            if (existingResourceIds.Contains(resourceId))
            {
                continue;
            }

            entity.BusyEventResources.Add(new BusyEventResources
            {
                TenantId = entity.TenantId,
                BusyEventId = entity.Id,
                ResourceId = resourceId
            });
        }

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<BusyEventManagementDto> SetActiveAsync(long busyEventId, bool isActive, CancellationToken ct)
    {
        var entity = await _dbContext.BusyEvents
            .Include(busyEvent => busyEvent.BusyEventResources)
            .FirstAsync(busyEvent => busyEvent.Id == busyEventId, ct)
            .ConfigureAwait(false);

        entity.IsActive = isActive;
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    private static BusyEventManagementDto Map(BusyEvents entity)
    {
        return new BusyEventManagementDto(
            entity.Id,
            entity.Title,
            DateTime.SpecifyKind(entity.StartUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(entity.EndUtc, DateTimeKind.Utc),
            entity.EventType,
            entity.BusyEventResources.Select(link => link.ResourceId).OrderBy(id => id).ToList(),
            entity.IsActive,
            entity.CreatedAtUtc);
    }
}
