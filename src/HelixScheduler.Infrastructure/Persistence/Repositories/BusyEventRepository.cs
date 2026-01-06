using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class BusyEventRepository : IBusyEventRepository
{
    private readonly SchedulerDbContext _dbContext;

    public BusyEventRepository(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<BusyEvents>> GetBusyAsync(
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<BusyEvents>();
        }

        var query = _dbContext.BusyEvents
            .AsNoTracking()
            .Where(busyEvent => busyEvent.BusyEventResources.Any(link => resourceIds.Contains(link.ResourceId)))
            .Where(busyEvent => busyEvent.StartUtc < toUtc && busyEvent.EndUtc > fromUtc);

        return await query
            .Include(busyEvent => busyEvent.BusyEventResources.Where(link => resourceIds.Contains(link.ResourceId)))
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
