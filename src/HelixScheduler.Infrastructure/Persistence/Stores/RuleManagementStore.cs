using HelixScheduler.Application.RuleManagement;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Stores;

public sealed class RuleManagementStore : IRuleManagementStore
{
    private readonly SchedulerDbContext _dbContext;

    public RuleManagementStore(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RuleManagementDto?> FindByIdAsync(long ruleId, CancellationToken ct)
    {
        var entity = await _dbContext.Rules
            .AsNoTracking()
            .Include(rule => rule.RuleResources)
            .FirstOrDefaultAsync(rule => rule.Id == ruleId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<RuleManagementDto>> ListAsync(CancellationToken ct)
    {
        var entities = await _dbContext.Rules
            .AsNoTracking()
            .Include(rule => rule.RuleResources)
            .OrderByDescending(rule => rule.CreatedAtUtc)
            .ThenByDescending(rule => rule.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(Map).ToList();
    }

    public async Task<RuleManagementDto> CreateAsync(
        Guid tenantId,
        RuleDefinition definition,
        DateTime createdAtUtc,
        CancellationToken ct)
    {
        var entity = new Rules
        {
            TenantId = tenantId,
            Kind = (byte)definition.Shape,
            IsExclude = definition.IsExclude,
            Title = definition.Title,
            FromDateUtc = definition.FromDateUtc,
            ToDateUtc = definition.ToDateUtc,
            SingleDateUtc = definition.SingleDateUtc,
            StartTime = definition.StartTime,
            EndTime = definition.EndTime,
            DaysOfWeekMask = definition.DaysOfWeekMask,
            DayOfMonth = definition.DayOfMonth,
            IntervalDays = definition.IntervalDays,
            IsActive = true,
            CreatedAtUtc = createdAtUtc
        };

        for (var i = 0; i < definition.ResourceIds.Count; i++)
        {
            entity.RuleResources.Add(new RuleResources
            {
                TenantId = tenantId,
                ResourceId = definition.ResourceIds[i]
            });
        }

        _dbContext.Rules.Add(entity);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<RuleManagementDto> UpdateAsync(long ruleId, RuleDefinition definition, CancellationToken ct)
    {
        var entity = await _dbContext.Rules
            .Include(rule => rule.RuleResources)
            .FirstAsync(rule => rule.Id == ruleId, ct)
            .ConfigureAwait(false);

        entity.Kind = (byte)definition.Shape;
        entity.IsExclude = definition.IsExclude;
        entity.Title = definition.Title;
        entity.FromDateUtc = definition.FromDateUtc;
        entity.ToDateUtc = definition.ToDateUtc;
        entity.SingleDateUtc = definition.SingleDateUtc;
        entity.StartTime = definition.StartTime;
        entity.EndTime = definition.EndTime;
        entity.DaysOfWeekMask = definition.DaysOfWeekMask;
        entity.DayOfMonth = definition.DayOfMonth;
        entity.IntervalDays = definition.IntervalDays;

        var existingResourceIds = entity.RuleResources.Select(link => link.ResourceId).ToHashSet();
        var targetResourceIds = definition.ResourceIds.ToHashSet();

        var toRemove = entity.RuleResources.Where(link => !targetResourceIds.Contains(link.ResourceId)).ToList();
        if (toRemove.Count > 0)
        {
            _dbContext.RuleResources.RemoveRange(toRemove);
        }

        foreach (var resourceId in definition.ResourceIds)
        {
            if (existingResourceIds.Contains(resourceId))
            {
                continue;
            }

            entity.RuleResources.Add(new RuleResources
            {
                TenantId = entity.TenantId,
                RuleId = entity.Id,
                ResourceId = resourceId
            });
        }

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    public async Task<RuleManagementDto> SetActiveAsync(long ruleId, bool isActive, CancellationToken ct)
    {
        var entity = await _dbContext.Rules
            .Include(rule => rule.RuleResources)
            .FirstAsync(rule => rule.Id == ruleId, ct)
            .ConfigureAwait(false);

        entity.IsActive = isActive;
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(entity);
    }

    private static RuleManagementDto Map(Rules entity)
    {
        return new RuleManagementDto(
            entity.Id,
            (RuleShape)entity.Kind,
            entity.IsExclude,
            entity.Title,
            entity.FromDateUtc,
            entity.ToDateUtc,
            entity.SingleDateUtc,
            entity.StartTime,
            entity.EndTime,
            entity.DaysOfWeekMask,
            entity.DayOfMonth,
            entity.IntervalDays,
            entity.RuleResources.Select(link => link.ResourceId).OrderBy(id => id).ToList(),
            entity.IsActive,
            entity.CreatedAtUtc);
    }
}
