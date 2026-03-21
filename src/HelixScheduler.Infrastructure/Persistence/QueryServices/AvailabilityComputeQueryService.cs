using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.QueryServices;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.QueryServices;

public sealed class AvailabilityComputeQueryService : IAvailabilityComputeQueryService
{
    private readonly SchedulerDbContext _dbContext;

    public AvailabilityComputeQueryService(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RuleData>> GetRulesAsync(
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<RuleData>();
        }

        var rows = await _dbContext.RuleResources
            .AsNoTracking()
            .Where(link => resourceIds.Contains(link.ResourceId))
            .Where(link => link.Rule.IsActive)
            .Where(link =>
                (link.Rule.SingleDateUtc != null && link.Rule.SingleDateUtc >= fromDateUtc && link.Rule.SingleDateUtc <= toDateUtc) ||
                (link.Rule.FromDateUtc != null && link.Rule.ToDateUtc != null && link.Rule.FromDateUtc <= toDateUtc && link.Rule.ToDateUtc >= fromDateUtc) ||
                (link.Rule.FromDateUtc != null && link.Rule.ToDateUtc == null && link.Rule.FromDateUtc <= toDateUtc) ||
                (link.Rule.FromDateUtc == null && link.Rule.ToDateUtc != null && link.Rule.ToDateUtc >= fromDateUtc) ||
                (link.Rule.FromDateUtc == null && link.Rule.ToDateUtc == null && link.Rule.SingleDateUtc == null))
            .Select(link => new
            {
                link.Rule.Id,
                link.Rule.Kind,
                link.Rule.IsExclude,
                link.Rule.FromDateUtc,
                link.Rule.ToDateUtc,
                link.Rule.SingleDateUtc,
                link.Rule.StartTime,
                link.Rule.EndTime,
                link.Rule.DaysOfWeekMask,
                link.Rule.DayOfMonth,
                link.Rule.IntervalDays,
                ResourceId = link.ResourceId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return Array.Empty<RuleData>();
        }

        var grouped = rows.GroupBy(row => new
        {
            row.Id,
            row.Kind,
            row.IsExclude,
            row.FromDateUtc,
            row.ToDateUtc,
            row.SingleDateUtc,
            row.StartTime,
            row.EndTime,
            row.DaysOfWeekMask,
            row.DayOfMonth,
            row.IntervalDays
        });

        var result = new List<RuleData>();
        foreach (var group in grouped)
        {
            result.Add(new RuleData(
                group.Key.Id,
                group.Key.Kind,
                group.Key.IsExclude,
                group.Key.FromDateUtc,
                group.Key.ToDateUtc,
                group.Key.SingleDateUtc,
                group.Key.StartTime,
                group.Key.EndTime,
                group.Key.DaysOfWeekMask,
                group.Key.DayOfMonth,
                group.Key.IntervalDays,
                group.Select(item => item.ResourceId).ToList()));
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<int, int>> GetResourceCapacitiesAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var capacities = await _dbContext.Resources
            .AsNoTracking()
            .Where(resource => resourceIds.Contains(resource.Id))
            .Select(resource => new { resource.Id, resource.Capacity })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var result = new Dictionary<int, int>(capacities.Count);
        for (var i = 0; i < capacities.Count; i++)
        {
            result[capacities[i].Id] = capacities[i].Capacity;
        }

        return result;
    }

    public async Task<IReadOnlyList<BusyEventData>> GetBusyEventsAsync(
        DateTime fromUtc,
        DateTime toUtcExclusive,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<BusyEventData>();
        }

        var rows = await _dbContext.BusyEventResources
            .AsNoTracking()
            .Where(link => resourceIds.Contains(link.ResourceId))
            .Where(link => link.BusyEvent.IsActive)
            .Where(link => link.BusyEvent.StartUtc < toUtcExclusive && link.BusyEvent.EndUtc > fromUtc)
            .Select(link => new
            {
                link.BusyEvent.Id,
                link.BusyEvent.StartUtc,
                link.BusyEvent.EndUtc,
                ResourceId = link.ResourceId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return Array.Empty<BusyEventData>();
        }

        var grouped = rows.GroupBy(row => new
        {
            row.Id,
            row.StartUtc,
            row.EndUtc
        });

        var result = new List<BusyEventData>();
        foreach (var group in grouped)
        {
            result.Add(new BusyEventData(
                group.Key.Id,
                group.Key.StartUtc,
                group.Key.EndUtc,
                group.Select(item => item.ResourceId).ToList()));
        }

        return result;
    }
}

