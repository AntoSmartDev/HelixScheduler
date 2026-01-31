using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class RuleRepository : IRuleRepository
{
    private readonly SchedulerDbContext _dbContext;

    public RuleRepository(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RuleRow>> GetRulesAsync(
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<RuleRow>();
        }

        var rows = await _dbContext.RuleResources
            .AsNoTracking()
            .Where(link => resourceIds.Contains(link.ResourceId))
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
                link.Rule.Title,
                link.Rule.FromDateUtc,
                link.Rule.ToDateUtc,
                link.Rule.SingleDateUtc,
                link.Rule.StartTime,
                link.Rule.EndTime,
                link.Rule.DaysOfWeekMask,
                link.Rule.DayOfMonth,
                link.Rule.IntervalDays,
                link.Rule.CreatedAtUtc,
                ResourceId = link.ResourceId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return Array.Empty<RuleRow>();
        }

        var grouped = rows.GroupBy(row => new
        {
            row.Id,
            row.Kind,
            row.IsExclude,
            row.Title,
            row.FromDateUtc,
            row.ToDateUtc,
            row.SingleDateUtc,
            row.StartTime,
            row.EndTime,
            row.DaysOfWeekMask,
            row.DayOfMonth,
            row.IntervalDays,
            row.CreatedAtUtc
        });

        var result = new List<RuleRow>();
        foreach (var group in grouped)
        {
            result.Add(new RuleRow(
                group.Key.Id,
                group.Key.Kind,
                group.Key.IsExclude,
                group.Key.Title,
                group.Key.FromDateUtc,
                group.Key.ToDateUtc,
                group.Key.SingleDateUtc,
                group.Key.StartTime,
                group.Key.EndTime,
                group.Key.DaysOfWeekMask,
                group.Key.DayOfMonth,
                group.Key.IntervalDays,
                group.Key.CreatedAtUtc,
                group.Select(item => item.ResourceId).ToList()));
        }

        return result;
    }
}
