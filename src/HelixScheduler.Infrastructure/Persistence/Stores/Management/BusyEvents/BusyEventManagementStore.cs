using HelixScheduler.Application.Management.BusyEvents;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using BusyEventEntity = HelixScheduler.Infrastructure.Persistence.Entities.BusyEvents;

namespace HelixScheduler.Infrastructure.Persistence.Stores.Management.BusyEvents;

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

    public async Task<BusyEventManagementDto?> FindByExternalKeyAsync(string externalKey, CancellationToken ct)
    {
        var entity = await _dbContext.BusyEvents
            .AsNoTracking()
            .Include(busyEvent => busyEvent.BusyEventResources)
            .FirstOrDefaultAsync(busyEvent => busyEvent.ExternalKey == externalKey, ct)
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
        var entity = new BusyEventEntity
        {
            TenantId = tenantId,
            Title = definition.Title,
            StartUtc = definition.StartUtc,
            EndUtc = definition.EndUtc,
            EventType = definition.EventType,
            ExternalKey = definition.ExternalKey,
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

    public async Task<IReadOnlyList<BusyEventManagementDto>> CreateManyAsync(
        Guid tenantId,
        IReadOnlyList<BusyEventDefinition> definitions,
        DateTime createdAtUtc,
        CancellationToken ct)
    {
        var entities = new List<BusyEventEntity>(definitions.Count);

        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            var entity = new BusyEventEntity
            {
                TenantId = tenantId,
                Title = definition.Title,
                StartUtc = definition.StartUtc,
                EndUtc = definition.EndUtc,
                EventType = definition.EventType,
                ExternalKey = definition.ExternalKey,
                IsActive = true,
                CreatedAtUtc = createdAtUtc
            };

            for (var j = 0; j < definition.ResourceIds.Count; j++)
            {
                entity.BusyEventResources.Add(new BusyEventResources
                {
                    TenantId = tenantId,
                    ResourceId = definition.ResourceIds[j]
                });
            }

            entities.Add(entity);
        }

        _dbContext.BusyEvents.AddRange(entities);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return entities.Select(Map).ToList();
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
        entity.ExternalKey = definition.ExternalKey;
        entity.IsActive = true;

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

    private static BusyEventManagementDto Map(BusyEventEntity entity)
    {
        return new BusyEventManagementDto(
            entity.Id,
            entity.Title,
            DateTime.SpecifyKind(entity.StartUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(entity.EndUtc, DateTimeKind.Utc),
            entity.EventType,
            entity.ExternalKey,
            entity.BusyEventResources.Select(link => link.ResourceId).OrderBy(id => id).ToList(),
            entity.IsActive,
            entity.CreatedAtUtc);
    }
}
