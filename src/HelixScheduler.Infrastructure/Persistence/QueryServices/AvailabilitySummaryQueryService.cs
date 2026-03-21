using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.QueryServices;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.QueryServices;

public sealed class AvailabilitySummaryQueryService : IAvailabilitySummaryQueryService
{
    private readonly SchedulerDbContext _dbContext;

    public AvailabilitySummaryQueryService(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ResourceSummary>> GetResourcesAsync(bool onlySchedulable, CancellationToken ct)
    {
        var query = _dbContext.Resources
            .AsNoTracking()
            .Where(resource => resource.IsActive && !resource.IsArchived && resource.Type.IsActive);
        if (onlySchedulable)
        {
            query = query.Where(resource => resource.IsSchedulable);
        }

        return await query
            .OrderBy(resource => resource.Name)
            .Select(resource => new ResourceSummary(
                resource.Id,
                resource.Code,
                resource.Name,
                resource.IsSchedulable))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RuleSummary>> GetRuleSummariesAsync(
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<RuleSummary>();
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
                link.Rule.Title,
                link.Rule.Kind,
                link.Rule.IsExclude,
                link.Rule.FromDateUtc,
                link.Rule.ToDateUtc,
                link.Rule.SingleDateUtc,
                link.Rule.StartTime,
                link.Rule.EndTime,
                link.Rule.DaysOfWeekMask,
                ResourceId = link.ResourceId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return Array.Empty<RuleSummary>();
        }

        var grouped = rows.GroupBy(row => new
        {
            row.Id,
            row.Title,
            row.Kind,
            row.IsExclude,
            row.FromDateUtc,
            row.ToDateUtc,
            row.SingleDateUtc,
            row.StartTime,
            row.EndTime,
            row.DaysOfWeekMask
        });

        var result = new List<RuleSummary>();
        foreach (var group in grouped)
        {
            result.Add(new RuleSummary(
                group.Key.Id,
                group.Key.Title,
                group.Key.Kind,
                group.Key.IsExclude,
                group.Key.FromDateUtc,
                group.Key.ToDateUtc,
                group.Key.SingleDateUtc,
                group.Key.StartTime,
                group.Key.EndTime,
                group.Key.DaysOfWeekMask,
                group.Select(item => item.ResourceId).ToList()));
        }

        return result;
    }

    public async Task<IReadOnlyList<BusyEventSummary>> GetBusyEventSummariesAsync(
        DateTime fromUtc,
        DateTime toUtcExclusive,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<BusyEventSummary>();
        }

        var rows = await _dbContext.BusyEventResources
            .AsNoTracking()
            .Where(link => resourceIds.Contains(link.ResourceId))
            .Where(link => link.BusyEvent.IsActive)
            .Where(link => link.BusyEvent.StartUtc < toUtcExclusive && link.BusyEvent.EndUtc > fromUtc)
            .Select(link => new
            {
                link.BusyEvent.Id,
                link.BusyEvent.Title,
                link.BusyEvent.StartUtc,
                link.BusyEvent.EndUtc,
                ResourceId = link.ResourceId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return Array.Empty<BusyEventSummary>();
        }

        var grouped = rows.GroupBy(row => new
        {
            row.Id,
            row.Title,
            row.StartUtc,
            row.EndUtc
        });

        var result = new List<BusyEventSummary>();
        foreach (var group in grouped)
        {
            result.Add(new BusyEventSummary(
                group.Key.Id,
                group.Key.Title,
                DateTime.SpecifyKind(group.Key.StartUtc, DateTimeKind.Utc),
                DateTime.SpecifyKind(group.Key.EndUtc, DateTimeKind.Utc),
                group.Select(item => item.ResourceId).ToList()));
        }

        return result;
    }
}

