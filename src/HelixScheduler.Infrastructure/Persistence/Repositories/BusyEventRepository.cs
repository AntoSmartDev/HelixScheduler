using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class BusyEventRepository : IBusyEventRepository
{
    private readonly SchedulerDbContext _dbContext;

    public BusyEventRepository(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<BusyEventRow>> GetBusyAsync(
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<BusyEventRow>();
        }

        var rows = await _dbContext.BusyEventResources
            .AsNoTracking()
            .Where(link => resourceIds.Contains(link.ResourceId))
            .Where(link => link.BusyEvent.StartUtc < toUtc && link.BusyEvent.EndUtc > fromUtc)
            .Select(link => new
            {
                link.BusyEvent.Id,
                link.BusyEvent.Title,
                link.BusyEvent.StartUtc,
                link.BusyEvent.EndUtc,
                link.BusyEvent.EventType,
                link.BusyEvent.CreatedAtUtc,
                ResourceId = link.ResourceId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return Array.Empty<BusyEventRow>();
        }

        var grouped = rows.GroupBy(row => new
        {
            row.Id,
            row.Title,
            row.StartUtc,
            row.EndUtc,
            row.EventType,
            row.CreatedAtUtc
        });

        var result = new List<BusyEventRow>();
        foreach (var group in grouped)
        {
            result.Add(new BusyEventRow(
                group.Key.Id,
                group.Key.Title,
                group.Key.StartUtc,
                group.Key.EndUtc,
                group.Key.EventType,
                group.Key.CreatedAtUtc,
                group.Select(item => item.ResourceId).ToList()));
        }

        return result;
    }
}
